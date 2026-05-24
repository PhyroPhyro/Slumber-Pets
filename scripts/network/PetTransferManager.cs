using Godot;
using System;
using System.Threading.Tasks;

public partial class PetTransferManager : Node
{
    [Signal] public delegate void OnTransferStartedEventHandler();
    [Signal] public delegate void OnTransferCompletedEventHandler();
    [Signal] public delegate void OnTransferFailedEventHandler(string reason);
    [Signal] public delegate void OnTransferProgressEventHandler(string status, float progress);
    [Signal] public delegate void OnNoRemotePetEventHandler();
    [Signal] public delegate void OnReceiveFailedEventHandler(string reason);

    private FirestoreService _firestoreService;
    private FirebaseAuthManager _authManager;
    private bool _isTransferring = false;
    private Timer _timeoutTimer;
    private const float TimeoutSeconds = 15.0f;
    private const int MaxRetries = 3;
    private const float BaseBackoffSeconds = 2.0f;

    public bool IsTransferring => _isTransferring;

    public override void _Ready()
    {
        _firestoreService = GetNode<FirestoreService>("/root/FirestoreService");
        _authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");

        _firestoreService.OnSnapshotWriteComplete += OnWriteComplete;
        _firestoreService.OnNetworkError += OnNetworkError;

        GD.Print("[PetTransferManager] Initialized (async transfer mode)");
    }

    public override void _ExitTree()
    {
        StopTransfer();
    }

    public async void SendPetToRemote(string targetLocation)
    {
        if (_isTransferring) return;

        if (!_authManager.IsAuthenticated)
        {
            EmitSignal(SignalName.OnTransferFailed, "Login required to transfer pet");
            return;
        }

        _isTransferring = true;
        EmitSignal(SignalName.OnTransferStarted);
        EmitSignal(SignalName.OnTransferProgress, "Enviando dados...", 0.25f);

        var gameManager = GetNode<GameManager>("/root/GameManager");
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        string transferId = Guid.NewGuid().ToString();

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

        GD.Print("[PetTransferManager] Sending pet to Firestore: transferId=", transferId, " target=", targetLocation);

        bool success = await _firestoreService.SendPetAsync(snapshot);

        if (success)
        {
            PetLocation newLoc = targetLocation == "mobile" ? PetLocation.Mobile : PetLocation.PC;
            petLocationManager.CompleteTransfer(newLoc);
            gameManager.SaveGame();

            _isTransferring = false;
            StopTimeout();
            EmitSignal(SignalName.OnTransferCompleted);
        }
        else
        {
            _isTransferring = false;
            petLocationManager.CancelTransfer();
            StopTimeout();
            EmitSignal(SignalName.OnTransferFailed, "Falha ao enviar pet. Verifique sua conexão.");
        }
    }

    public void SendPetToMobile()
    {
        SendPetToRemote("mobile");
    }

    public void SendPetToPC()
    {
        SendPetToRemote("pc");
    }

    public async void TryReceivePet()
    {
        await TryReceivePetWithRetry(0);
    }

    private async Task TryReceivePetWithRetry(int attempt)
    {
        if (_isTransferring) return;

        if (!_authManager.IsAuthenticated)
        {
            EmitSignal(SignalName.OnReceiveFailed, "Login required to receive pet");
            return;
        }

        _isTransferring = true;
        EmitSignal(SignalName.OnTransferStarted);
        EmitSignal(SignalName.OnTransferProgress, "Verificando pet remoto...", 0.25f);

        StartTimeout();

        RemotePetStatus status = await _firestoreService.CheckRemotePetAsync();

        if (status == RemotePetStatus.None)
        {
            _isTransferring = false;
            StopTimeout();
            EmitSignal(SignalName.OnNoRemotePet);
            return;
        }

        if (status == RemotePetStatus.Claimed)
        {
            _isTransferring = false;
            StopTimeout();
            EmitSignal(SignalName.OnNoRemotePet);
            return;
        }

        if (status == RemotePetStatus.Error)
        {
            if (attempt < MaxRetries)
            {
                float backoff = (float)(BaseBackoffSeconds * Math.Pow(2, attempt));
                GD.Print("[PetTransferManager] Network error, retry in ", backoff, "s (attempt ", attempt + 1, "/", MaxRetries, ")");
                EmitSignal(SignalName.OnTransferProgress, "Erro de conexão. Tentando novamente...", 0.3f);
                StopTimeout();
                await Task.Delay((int)(backoff * 1000));
                _isTransferring = false;
                await TryReceivePetWithRetry(attempt + 1);
                return;
            }

            _isTransferring = false;
            StopTimeout();
            EmitSignal(SignalName.OnReceiveFailed, "Não foi possível conectar ao servidor após várias tentativas. Verifique sua internet.");
            return;
        }

        EmitSignal(SignalName.OnTransferProgress, "Pet encontrado! Recebendo...", 0.5f);

        PetSnapshot snapshot = await _firestoreService.ClaimRemotePetAsync();

        if (snapshot == null)
        {
            if (attempt < MaxRetries)
            {
                float backoff = (float)(BaseBackoffSeconds * Math.Pow(2, attempt));
                GD.Print("[PetTransferManager] Claim failed, retry in ", backoff, "s (attempt ", attempt + 1, "/", MaxRetries, ")");
                EmitSignal(SignalName.OnTransferProgress, "Falha ao receber. Tentando novamente...", 0.6f);
                StopTimeout();
                await Task.Delay((int)(backoff * 1000));
                _isTransferring = false;
                await TryReceivePetWithRetry(attempt + 1);
                return;
            }

            _isTransferring = false;
            StopTimeout();
            EmitSignal(SignalName.OnReceiveFailed, "Não foi possível receber o pet. Tente novamente mais tarde.");
            return;
        }

        EmitSignal(SignalName.OnTransferProgress, "Processando dados...", 0.75f);

        try
        {
            var gameManager = GetNode<GameManager>("/root/GameManager");
            var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");

            gameManager.CurrentSave.Pet = snapshot.Pet;
            gameManager.CurrentSave.Inventory = snapshot.Inventory;

            foreach (var report in snapshot.AccumulatedReports.SleepReports)
            {
                if (!gameManager.CurrentSave.SleepHistory.Exists(r => r.Date == report.Date))
                    gameManager.CurrentSave.SleepHistory.Add(report);
            }

            foreach (var report in snapshot.AccumulatedReports.WalkReports)
            {
                if (!gameManager.CurrentSave.WalkHistory.Exists(r => r.Date == report.Date))
                    gameManager.CurrentSave.WalkHistory.Add(report);
            }

            gameManager.CurrentSave.LastTransferId = snapshot.TransferId;
            gameManager.CurrentSave.PetLocation = snapshot.PetLocation;

            PetLocation newLoc = snapshot.PetLocation == "mobile" ? PetLocation.Mobile : PetLocation.PC;
            petLocationManager.ReceiveTransfer(newLoc);

            gameManager.SaveGame();

            _isTransferring = false;
            StopTimeout();
            EmitSignal(SignalName.OnTransferCompleted);
            GD.Print("[PetTransferManager] Pet received successfully from Firestore");
        }
        catch (Exception e)
        {
            GD.PrintErr("[PetTransferManager] Error receiving pet: ", e.Message);
            _isTransferring = false;
            StopTimeout();
            EmitSignal(SignalName.OnReceiveFailed, "Erro ao processar pet recebido.");
        }
    }

    private void OnWriteComplete()
    {
        GD.Print("[PetTransferManager] PetSnapshot written to Firestore");
    }

    private void OnNetworkError(string reason)
    {
        GD.PrintErr("[PetTransferManager] Network error: ", reason);
        if (_isTransferring)
        {
            EmitSignal(SignalName.OnTransferFailed, "Network error: " + reason);
        }
    }

    private void StartTimeout()
    {
        _timeoutTimer?.Stop();
        _timeoutTimer?.QueueFree();
        _timeoutTimer = new Timer();
        _timeoutTimer.WaitTime = TimeoutSeconds;
        _timeoutTimer.OneShot = true;
        _timeoutTimer.Timeout += OnTimeout;
        AddChild(_timeoutTimer);
        _timeoutTimer.Start();
        GD.Print("[PetTransferManager] Timeout started: ", TimeoutSeconds, "s");
    }

    private void StopTimeout()
    {
        _timeoutTimer?.Stop();
        _timeoutTimer?.QueueFree();
        _timeoutTimer = null;
    }

    private void OnTimeout()
    {
        if (_isTransferring)
        {
            GD.PrintErr("[PetTransferManager] Transfer timeout");
            _isTransferring = false;
            EmitSignal(SignalName.OnReceiveFailed, "Tempo esgotado. Não foi possível conectar ao servidor.");
        }
    }

    public void StopTransfer()
    {
        StopTimeout();
        _isTransferring = false;
        GD.Print("[PetTransferManager] Transfer stopped");
    }
}
