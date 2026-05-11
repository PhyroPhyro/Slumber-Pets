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
public override void _Ready()
{
_lanDiscovery = GetNode<LanDiscovery>("/root/LanDiscovery");
_syncHost = GetNode<SyncHost>("/root/SyncHost");
_syncClient = GetNode<SyncClient>("/root/SyncClient");
_lanDiscovery.OnHostDiscovered += OnHostDiscovered;
_syncHost.OnClientConnected += OnClientConnected;
_syncHost.OnDataReceived += OnHostDataReceived;
_syncClient.OnConnected += OnClientConnectedToHost;
_syncClient.OnDataReceived += OnClientDataReceived;
_syncClient.OnConnectionFailed += OnClientConnectionFailed;
GD.Print("[PetTransferManager] Initialized");
}
public void StartSendingAsHost(string pin)
{
_lanDiscovery.StartBroadcasting(pin, "pc");
_syncHost.StartListening(_lanDiscovery.DiscoveryTcpPort);
GD.Print("[PetTransferManager] Started as host, waiting for mobile...");
}
    public void StartReceivingAsClient(string pcIp, int pcPort)
    {
        _targetIp = pcIp;
        _targetPort = pcPort;
        _isMobileReceiving = true;
        _syncClient.ConnectToHost(pcIp, pcPort);
        GD.Print("[PetTransferManager] Connecting to PC to receive pet: ", pcIp, ":", pcPort);
    }

    public void ConnectToHost(string ip, int port, string pin)
{
_targetIp = ip;
_targetPort = port;
_syncClient.ConnectToHost(ip, port);
GD.Print("[PetTransferManager] Connecting to host: ", ip, ":", port);
}
private void OnHostDiscovered(string ip, int port, string pin, string petLocation)
{
GD.Print("[PetTransferManager] Host discovered: ", ip, ":", port, " Pet location: ", petLocation);
}
private void OnClientConnected()
{
GD.Print("[PetTransferManager] Client connected to host");
}
    private void OnHostDataReceived(string data)
    {
        if (data.StartsWith("PAIR_REQUEST"))
        {
            GD.Print("[PetTransferManager] Mobile pairing request received");
            _syncHost.SendData("PAIR_ACK|accepted:true");
            GD.Print("[PetTransferManager] Pair acknowledged - sending pet");
            SendPetToMobile();
        }
        else if (data.StartsWith("SEND_PET"))
        {
            string petData = data.Substring("SEND_PET|".Length);
            ReceivePet(petData);
            _syncHost.SendData("PET_ACK|received:true|timestamp:" + DateTime.UtcNow.ToString("o"));
        }
        else if (data.StartsWith("PET_ACK"))
        {
            CompleteTransfer();
        }
    }
private void OnClientConnectedToHost()
{
GD.Print("[PetTransferManager] Connected to host");
_syncClient.SendData("PAIR_REQUEST|pin:default|deviceId:mobile01");
}
    private void OnClientDataReceived(string data)
    {
        if (data.StartsWith("PAIR_ACK"))
        {
            if (_isMobileReceiving)
            {
                GD.Print("[PetTransferManager] Mobile received PAIR_ACK - waiting for PC to send pet");
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
    }
private void OnClientConnectionFailed(string reason)
{
EmitSignal(SignalName.OnTransferFailed, reason);
GD.PrintErr("[PetTransferManager] Connection failed: ", reason);
}
    private void SendPetToRemote()
    {
        if (_isTransferring) return;
        _isTransferring = true;
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
        _syncClient.SendData("SEND_PET|" + json);
        GD.Print("[PetTransferManager] Sent pet snapshot");
    }

    private void SendPetToMobile()
    {
        if (_isTransferring) return;
        _isTransferring = true;
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
            gameManager.CurrentSave.PetLocation = "mobile";
            petLocationManager.ReceiveTransfer();
            gameManager.SaveGame();
            GD.Print("[PetTransferManager] Pet received and saved");

            if (_isMobileReceiving)
            {
                _isMobileReceiving = false;
                _isTransferring = false;
                EmitSignal(SignalName.OnTransferCompleted);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[PetTransferManager] Error receiving pet: ", e.Message);
        }
    }
    private void CompleteTransfer()
    {
        _isTransferring = false;
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        petLocationManager.CompleteTransfer();
        var gameManager = GetNode<GameManager>("/root/GameManager");

        if (_isMobileReceiving)
        {
            gameManager.CurrentSave.PetLocation = "mobile";
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
        _lanDiscovery.StopBroadcasting();
        _syncHost.Stop();
        _syncClient.Disconnect();
        _isTransferring = false;
        _isMobileReceiving = false;
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