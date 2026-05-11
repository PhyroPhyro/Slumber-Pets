using Godot;
using System;

public partial class PetTransferManager : Node
{
    [Signal] public delegate void OnTransferStartedEventHandler();
    [Signal] public delegate void OnTransferCompletedEventHandler();
    [Signal] public delegate void OnTransferFailedEventHandler(string reason);

    private LanDiscovery _lanDiscovery;
    private SyncHost _syncHost;
    private SyncClient _syncClient;
    private string _targetIp = "";
    private int _targetPort = 0;
    private bool _isTransferring = false;
    private bool _isMobileReceiving = false;
    private bool _isMobileSending = false;
    private bool _isConnected = false;
    private Timer _connectionTimeoutTimer;
    private const float ConnectionTimeoutSeconds = 15.0f;

    public override void _Ready()
    {
        _lanDiscovery = GetNode<LanDiscovery>("/root/LanDiscovery");
        _syncHost = GetNode<SyncHost>("/root/SyncHost");
        _syncClient = GetNode<SyncClient>("/root/SyncClient");

        _lanDiscovery.OnHostDiscovered += OnHostDiscovered;
        _syncHost.OnClientConnected += OnHostClientConnected;
        _syncHost.OnDataReceived += OnHostDataReceived;
        _syncClient.OnConnected += OnClientConnectedToHost;
        _syncClient.OnDataReceived += OnClientDataReceived;
        _syncClient.OnConnectionFailed += OnClientConnectionFailed;
        _syncClient.OnDisconnected += OnClientDisconnected;

        GD.Print("[PetTransferManager] Initialized");
    }

    public override void _ExitTree()
    {
        StopTransfer();
    }

    public void StartSendingAsHost(string pin)
    {
        GD.Print("[PetTransferManager] Starting as host (PC sending to mobile)");
        _lanDiscovery.StartBroadcasting(pin, "pc");
        _syncHost.StartListening(_lanDiscovery.DiscoveryTcpPort);
        StartConnectionTimeout();
        GD.Print("[PetTransferManager] Started as host, waiting for mobile on port ", _lanDiscovery.DiscoveryTcpPort);
    }

    public void StartReceivingAsClient(string pcIp, int pcPort)
    {
        _targetIp = pcIp;
        _targetPort = pcPort;
        _isMobileReceiving = true;
        _isConnected = false;

        GD.Print("[PetTransferManager] Mobile connecting to PC to receive pet: ", pcIp, ":", pcPort);
        _syncClient.ConnectToHost(pcIp, pcPort);
        StartConnectionTimeout();
    }

    public void StartSendingAsMobileHost(string pin)
    {
        _lanDiscovery.StartBroadcasting(pin, "mobile");
        _syncHost.StartListening(_lanDiscovery.DiscoveryTcpPort);
        _isMobileSending = true;

        GD.Print("[PetTransferManager] Mobile started as host, waiting for PC to connect on port ", _lanDiscovery.DiscoveryTcpPort);
        StartConnectionTimeout();
    }

    public void StartReceivingFromMobile(string mobileIp, int mobilePort)
    {
        _targetIp = mobileIp;
        _targetPort = mobilePort;
        _isMobileReceiving = false;
        _isConnected = false;

        GD.Print("[PetTransferManager] PC connecting to mobile to receive pet: ", mobileIp, ":", mobilePort);
        _syncClient.ConnectToHost(mobileIp, mobilePort);
        StartConnectionTimeout();
    }

    public void ConnectToHost(string ip, int port, string pin)
    {
        _targetIp = ip;
        _targetPort = port;
        _isConnected = false;

        GD.Print("[PetTransferManager] Connecting to host: ", ip, ":", port);
        _syncClient.ConnectToHost(ip, port);
        StartConnectionTimeout();
    }

    private void StartConnectionTimeout()
    {
        _connectionTimeoutTimer?.Stop();
        _connectionTimeoutTimer?.QueueFree();
        _connectionTimeoutTimer = new Timer();
        _connectionTimeoutTimer.WaitTime = ConnectionTimeoutSeconds;
        _connectionTimeoutTimer.OneShot = true;
        _connectionTimeoutTimer.Timeout += OnConnectionTimeout;
        AddChild(_connectionTimeoutTimer);
        _connectionTimeoutTimer.Start();
        GD.Print("[PetTransferManager] Connection timeout started: ", ConnectionTimeoutSeconds, "s");
    }

    private void StopConnectionTimeout()
    {
        _connectionTimeoutTimer?.Stop();
        _connectionTimeoutTimer?.QueueFree();
        _connectionTimeoutTimer = null;
    }

    private void OnConnectionTimeout()
    {
        if (!_isConnected && !_isTransferring)
        {
            GD.PrintErr("[PetTransferManager] Connection timeout after ", ConnectionTimeoutSeconds, "s");
            EmitSignal(SignalName.OnTransferFailed, "Connection timeout - verify firewall settings and ensure both devices are on the same network");
            StopTransfer();
        }
    }

    private void OnHostDiscovered(string ip, int port, string pin, string petLocation)
    {
        GD.Print("[PetTransferManager] Host discovered: ", ip, ":", port, " Pet location: ", petLocation);
    }

    private void OnHostClientConnected()
    {
        GD.Print("[PetTransferManager] Client connected to host");
        _isConnected = true;
        StopConnectionTimeout();
    }

    private void OnHostDataReceived(string data)
    {
        GD.Print("[PetTransferManager] Host received: ", data.Substring(0, Math.Min(data.Length, 50)));

        if (data.StartsWith("PAIR_REQUEST"))
        {
            GD.Print("[PetTransferManager] Pairing request received");
            _syncHost.SendData("PAIR_ACK|accepted:true");
            GD.Print("[PetTransferManager] Pair acknowledged - waiting for ready signal");
        }
        else if (data.StartsWith("MOBILE_READY") || data.StartsWith("PC_READY"))
        {
            GD.Print("[PetTransferManager] Remote is ready - sending pet");
            if (_isMobileSending)
            {
                SendPetToRemote();
            }
            else
            {
                SendPetToMobile();
            }
        }
        else if (data.StartsWith("SEND_PET"))
        {
            string petData = data.Substring("SEND_PET|".Length);
            ReceivePet(petData);
            _syncHost.SendData("PET_ACK|received:true|timestamp:" + DateTime.UtcNow.ToString("o"));
        }
        else if (data.StartsWith("PET_ACK"))
        {
            _syncHost.SendData("TRANSFER_CONFIRM|status:complete");
            CompleteTransfer();
        }
        else if (data.StartsWith("TRANSFER_CONFIRM"))
        {
            CompleteTransfer();
        }
    }

    private void OnClientConnectedToHost()
    {
        GD.Print("[PetTransferManager] Connected to host");
        _isConnected = true;
        StopConnectionTimeout();

        if (_isMobileSending)
        {
            _syncClient.SendData("PAIR_REQUEST|pin:default|deviceId:pc01");
        }
        else
        {
            _syncClient.SendData("PAIR_REQUEST|pin:default|deviceId:mobile01");
        }
    }

    private void OnClientDataReceived(string data)
    {
        GD.Print("[PetTransferManager] Client received: ", data.Substring(0, Math.Min(data.Length, 50)));

        if (data.StartsWith("PAIR_ACK"))
        {
            if (_isMobileReceiving)
            {
                GD.Print("[PetTransferManager] Received PAIR_ACK - sending ready signal");
                _syncClient.SendData("MOBILE_READY|deviceId:mobile01");
            }
            else if (_isMobileSending)
            {
                GD.Print("[PetTransferManager] PC received PAIR_ACK - sending ready signal");
                _syncClient.SendData("PC_READY|deviceId:pc01");
            }
            else
            {
                SendPetToRemote();
            }
        }
        else if (data.StartsWith("PET_ACK"))
        {
            CompleteTransfer();
        }
        else if (data.StartsWith("SEND_PET"))
        {
            string petData = data.Substring("SEND_PET|".Length);
            ReceivePet(petData);
            _syncClient.SendData("PET_ACK|received:true|timestamp:" + DateTime.UtcNow.ToString("o"));
            EmitSignal(SignalName.OnTransferStarted);
        }
        else if (data.StartsWith("TRANSFER_CONFIRM"))
        {
            CompleteTransfer();
        }
    }

    private void OnClientConnectionFailed(string reason)
    {
        GD.PrintErr("[PetTransferManager] Connection failed: ", reason);
        EmitSignal(SignalName.OnTransferFailed, reason);
        StopTransfer();
    }

    private void OnClientDisconnected()
    {
        GD.Print("[PetTransferManager] Client disconnected");
        _isConnected = false;
    }

    private void SendPetToRemote()
    {
        if (_isTransferring) return;
        _isTransferring = true;
        StopConnectionTimeout();

        EmitSignal(SignalName.OnTransferStarted);

        var gameManager = GetNode<GameManager>("/root/GameManager");
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        string transferId = Guid.NewGuid().ToString();
        petLocationManager.StartTransfer(transferId);

        PetSnapshot snapshot = new PetSnapshot
        {
            Version = 1,
            TransferId = transferId,
            PetLocation = "pc",
            TransferredAt = DateTime.UtcNow,
            Pet = gameManager.GetPetState().Clone(),
            Inventory = gameManager.CurrentSave.Inventory,
            AccumulatedReports = new AccumulatedReports
            {
                SleepReports = gameManager.CurrentSave.SleepHistory,
                WalkReports = gameManager.CurrentSave.WalkHistory
            }
        };

        string json = JsonSerialization.ToJson(snapshot);
        _syncClient.SendData("SEND_PET|" + json);
        GD.Print("[PetTransferManager] Sent pet snapshot to PC");
    }

    private void SendPetToMobile()
    {
        if (_isTransferring) return;
        _isTransferring = true;
        StopConnectionTimeout();

        EmitSignal(SignalName.OnTransferStarted);

        var gameManager = GetNode<GameManager>("/root/GameManager");
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        string transferId = Guid.NewGuid().ToString();
        petLocationManager.StartTransfer(transferId);

        PetSnapshot snapshot = new PetSnapshot
        {
            Version = 1,
            TransferId = transferId,
            PetLocation = "mobile",
            TransferredAt = DateTime.UtcNow,
            Pet = gameManager.GetPetState().Clone(),
            Inventory = gameManager.CurrentSave.Inventory,
            AccumulatedReports = new AccumulatedReports
            {
                SleepReports = gameManager.CurrentSave.SleepHistory,
                WalkReports = gameManager.CurrentSave.WalkHistory
            }
        };

        string json = JsonSerialization.ToJson(snapshot);
        _syncHost.SendData("SEND_PET|" + json);
        GD.Print("[PetTransferManager] Sent pet snapshot to mobile");
    }

    private void ReceivePet(string petData)
    {
        try
        {
            PetSnapshot snapshot = JsonSerialization.FromJson<PetSnapshot>(petData);
            if (snapshot == null) return;

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
            petLocationManager.ReceiveTransfer();
            gameManager.SaveGame();
            GD.Print("[PetTransferManager] Pet received and saved");
        }
        catch (Exception e)
        {
            GD.PrintErr("[PetTransferManager] Error receiving pet: ", e.Message);
        }
    }

    private void CompleteTransfer()
    {
        _isTransferring = false;
        StopConnectionTimeout();

        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        petLocationManager.CompleteTransfer();
        var gameManager = GetNode<GameManager>("/root/GameManager");

        if (_isMobileSending)
        {
            gameManager.CurrentSave.PetLocation = "pc";
        }
        else
        {
            gameManager.CurrentSave.PetLocation = "mobile";
        }

        gameManager.SaveGame();
        EmitSignal(SignalName.OnTransferCompleted);
        GD.Print("[PetTransferManager] Transfer completed");
    }

    public void StopTransfer()
    {
        StopConnectionTimeout();
        _lanDiscovery.StopBroadcasting();
        _syncHost.Stop();
        _syncClient.Disconnect();
        _isTransferring = false;
        _isMobileReceiving = false;
        _isMobileSending = false;
        _isConnected = false;
        GD.Print("[PetTransferManager] Transfer stopped");
    }
}

public class PetSnapshot
{
    public int Version { get; set; } = 1;
    public string TransferId { get; set; } = string.Empty;
    public string PetLocation { get; set; } = "pc";
    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public PetState Pet { get; set; } = new();
    public InventoryData Inventory { get; set; } = new();
    public AccumulatedReports AccumulatedReports { get; set; } = new();
}

public class AccumulatedReports
{
    public System.Collections.Generic.List<SleepReportData> SleepReports { get; set; } = new();
    public System.Collections.Generic.List<WalkReportData> WalkReports { get; set; } = new();
}
