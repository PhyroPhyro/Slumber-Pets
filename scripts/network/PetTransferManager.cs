using Godot;
using System;

public partial class PetTransferManager : Node
{
    [Signal] public delegate void OnTransferStartedEventHandler();
    [Signal] public delegate void OnTransferCompletedEventHandler();
    [Signal] public delegate void OnTransferFailedEventHandler(string reason);

    private FirestoreService _firestoreService;
    private FirebaseAuthManager _authManager;
    private bool _isTransferring = false;
    private string _pendingTransferId = "";
    private string _pendingTargetLocation = "";
    private Timer _transferTimeoutTimer;
    private const float TransferTimeoutSeconds = 30.0f;

    public bool IsTransferring => _isTransferring;

    public override void _Ready()
    {
        _firestoreService = GetNode<FirestoreService>("/root/FirestoreService");
        _authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");

        _firestoreService.OnSnapshotWriteComplete += OnWriteComplete;
        _firestoreService.OnSnapshotUpdated += OnSnapshotReceived;
        _firestoreService.OnNetworkError += OnNetworkError;

        GD.Print("[PetTransferManager] Initialized (Firebase mode)");
    }

    public override void _ExitTree()
    {
        StopTransfer();
    }

    public void SendPetToRemote(string targetLocation)
    {
        if (_isTransferring) return;

        if (!_authManager.IsAuthenticated)
        {
            EmitSignal(SignalName.OnTransferFailed, "Login required to transfer pet");
            return;
        }

        _isTransferring = true;
        _pendingTargetLocation = targetLocation;
        EmitSignal(SignalName.OnTransferStarted);

        var gameManager = GetNode<GameManager>("/root/GameManager");
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        string transferId = Guid.NewGuid().ToString();
        _pendingTransferId = transferId;
        petLocationManager.StartTransfer(transferId);

        PetSnapshot snapshot = new PetSnapshot
        {
            Version = 1,
            TransferId = transferId,
            PetLocation = targetLocation,
            TransferredAt = DateTime.UtcNow,
            Pet = gameManager.GetPetState().Clone(),
            Inventory = gameManager.CurrentSave.Inventory,
            AccumulatedReports = new AccumulatedReports
            {
                SleepReports = gameManager.CurrentSave.SleepHistory,
                WalkReports = gameManager.CurrentSave.WalkHistory
            }
        };

        GD.Print("[PetTransferManager] Writing PetSnapshot to Firestore: transferId=", transferId, " target=", targetLocation);
        _firestoreService.SetPetSnapshot(snapshot);
        StartTransferTimeout();
    }

    public void SendPetToMobile()
    {
        SendPetToRemote("mobile");
    }

    public void SendPetToPC()
    {
        SendPetToRemote("pc");
    }

    public void StartListeningForIncoming()
    {
        if (!_authManager.IsAuthenticated)
        {
            GD.Print("[PetTransferManager] Not logged in, cannot listen for incoming transfers");
            return;
        }

        GD.Print("[PetTransferManager] Listening for incoming pet transfer...");
        _firestoreService.StartListening();
    }

    private void OnWriteComplete()
    {
        GD.Print("[PetTransferManager] PetSnapshot written to Firestore, waiting for receiver confirmation...");
        StopTransferTimeout();
        _firestoreService.StartListening();
        GD.Print("[PetTransferManager] Now polling for confirmation from receiver");
    }

    private void OnSnapshotReceived()
    {
        PetSnapshot snapshot = _firestoreService.LastReceivedSnapshot;
        if (snapshot == null || string.IsNullOrEmpty(snapshot.TransferId))
            return;

        var gameManager = GetNode<GameManager>("/root/GameManager");
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");

        if (snapshot.TransferId == _pendingTransferId)
        {
            if (snapshot.Status == "confirmed")
            {
                GD.Print("[PetTransferManager] Receiver confirmed! Completing transfer...");
                PetLocation newLoc = _pendingTargetLocation == "mobile" ? PetLocation.Mobile : PetLocation.PC;
                petLocationManager.CompleteTransfer(newLoc);
                
                gameManager.SaveGame();
                
                _isTransferring = false;
                _pendingTransferId = "";
                _pendingTargetLocation = "";
                StopTransferTimeout();
                _firestoreService.StopListening();
                EmitSignal(SignalName.OnTransferCompleted);
            }
            else
            {
                GD.Print("[PetTransferManager] Received own snapshot (status=", snapshot.Status, "), waiting for confirmation...");
            }
            return;
        }

        if (snapshot.TransferId == gameManager.CurrentSave.LastTransferId)
        {
            GD.Print("[PetTransferManager] Snapshot already processed (transferId=", snapshot.TransferId, "), ignoring");
            return;
        }

        if (snapshot.Status == "confirmed")
        {
            GD.Print("[PetTransferManager] Snapshot already confirmed by someone else, ignoring");
            return;
        }

        string currentSaveLocation = gameManager.CurrentSave.PetLocation.ToLower();
        if (snapshot.PetLocation.ToLower() == currentSaveLocation)
        {
            GD.Print("[PetTransferManager] Snapshot location matches current save (", currentSaveLocation, "), ignoring");
            return;
        }

        GD.Print("[PetTransferManager] Snapshot is for this device! Current: ", currentSaveLocation, " Target: ", snapshot.PetLocation);
        GD.Print("[PetTransferManager] New snapshot detected: transferId=", snapshot.TransferId, " location=", snapshot.PetLocation, " status=", snapshot.Status);
        ReceivePet(snapshot);
    }

    private void ReceivePet(PetSnapshot snapshot)
    {
        GD.Print("[PetTransferManager] ReceivePet called for transferId=", snapshot.TransferId);
        try
        {
            var gameManager = GetNode<GameManager>("/root/GameManager");
            var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");

            gameManager.CurrentSave.Pet = snapshot.Pet;
            gameManager.CurrentSave.Inventory = snapshot.Inventory;

            foreach (var report in snapshot.AccumulatedReports.SleepReports)
            {
                if (!gameManager.CurrentSave.SleepHistory.Exists(r => r.Date == report.Date))
                {
                    gameManager.CurrentSave.SleepHistory.Add(report);
                }
            }

            foreach (var report in snapshot.AccumulatedReports.WalkReports)
            {
                if (!gameManager.CurrentSave.WalkHistory.Exists(r => r.Date == report.Date))
                {
                    gameManager.CurrentSave.WalkHistory.Add(report);
                }
            }

            gameManager.CurrentSave.LastTransferId = snapshot.TransferId;
            gameManager.CurrentSave.PetLocation = snapshot.PetLocation;
            
            PetLocation newLoc = snapshot.PetLocation == "mobile" ? PetLocation.Mobile : PetLocation.PC;
            petLocationManager.ReceiveTransfer(newLoc);
            
            gameManager.SaveGame();

            GD.Print("[PetTransferManager] Writing confirmation for transferId=", snapshot.TransferId);
            _firestoreService.WriteConfirmation(snapshot.TransferId);

            _isTransferring = false;
            StopTransferTimeout();
            _firestoreService.StopListening();
            EmitSignal(SignalName.OnTransferCompleted);
            GD.Print("[PetTransferManager] Pet received and saved from Firestore");
        }
        catch (Exception e)
        {
            GD.PrintErr("[PetTransferManager] Error receiving pet: ", e.Message);
            EmitSignal(SignalName.OnTransferFailed, "Error processing received pet");
        }
    }

    private void OnNetworkError(string reason)
    {
        GD.PrintErr("[PetTransferManager] Network error: ", reason);
        if (_isTransferring)
        {
            EmitSignal(SignalName.OnTransferFailed, "Network error: " + reason);
        }
    }

    private void StartTransferTimeout()
    {
        _transferTimeoutTimer?.Stop();
        _transferTimeoutTimer?.QueueFree();
        _transferTimeoutTimer = new Timer();
        _transferTimeoutTimer.WaitTime = TransferTimeoutSeconds;
        _transferTimeoutTimer.OneShot = true;
        _transferTimeoutTimer.Timeout += OnTransferTimeout;
        AddChild(_transferTimeoutTimer);
        _transferTimeoutTimer.Start();
        GD.Print("[PetTransferManager] Transfer timeout started: ", TransferTimeoutSeconds, "s");
    }

    private void StopTransferTimeout()
    {
        _transferTimeoutTimer?.Stop();
        _transferTimeoutTimer?.QueueFree();
        _transferTimeoutTimer = null;
    }

    private void OnTransferTimeout()
    {
        if (_isTransferring)
        {
            GD.PrintErr("[PetTransferManager] Transfer timeout after ", TransferTimeoutSeconds, "s");
            EmitSignal(SignalName.OnTransferFailed, "Transfer timeout — check your connection");
            StopTransfer();
        }
    }

    public void StopTransfer()
    {
        StopTransferTimeout();
        _firestoreService.StopListening();
        _isTransferring = false;
        _pendingTransferId = "";
        _pendingTargetLocation = "";
        GD.Print("[PetTransferManager] Transfer stopped");
    }
}
