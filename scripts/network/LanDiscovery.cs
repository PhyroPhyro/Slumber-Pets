using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class LanDiscovery : Node
{
    [Signal] public delegate void OnHostDiscoveredEventHandler(string ip, int port, string pin, string petLocation);

    private const int BroadcastPort = 18888;
    private const int TcpPort = 18889;
    private const string AnnounceMessage = "SLUMBER_PETS_ANNOUNCE";

    private UdpClient _udpBroadcastClient;
    private UdpClient _udpListenClient;
    private CancellationTokenSource _broadcastCts;
    private CancellationTokenSource _listenCTS;
    private bool _isBroadcasting = false;
    private bool _isListening = false;

    public int DiscoveryTcpPort => TcpPort;

    public override void _Ready()
    {
        GD.Print("[LanDiscovery] Initialized on port ", BroadcastPort);
    }

    public override void _ExitTree()
    {
        StopBroadcasting();
        StopListening();
    }

    public void StartBroadcasting(string pin, string petLocation)
    {
        if (_isBroadcasting) return;

        _isBroadcasting = true;
        _broadcastCTS = new CancellationTokenSource();
        string localIp = GetLocalIPAddress();

        GD.Print("[LanDiscovery] Started broadcasting - My IP: ", localIp, " Port: ", TcpPort);

        Task.Run(async () =>
        {
            _udpBroadcastClient = new UdpClient();
            _udpBroadcastClient.EnableBroadcast = true;

            try
            {
                while (_isBroadcasting && !_broadcastCTS.Token.IsCancellationRequested)
                {
                    try
                    {
                        string message = $"{AnnounceMessage}|{localIp}|{TcpPort}|{pin}|{petLocation}";
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        IPEndPoint broadcastEp = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
                        await _udpBroadcastClient.SendAsync(data, data.Length, broadcastEp);
                        GD.Print("[LanDiscovery] Broadcast: ", message);
                    }
                    catch (Exception e)
                    {
                        if (!_broadcastCTS.Token.IsCancellationRequested)
                            GD.PrintErr("[LanDiscovery] Broadcast error: ", e.Message);
                    }

                    try
                    {
                        await Task.Delay(2000, _broadcastCTS.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                _udpBroadcastClient?.Close();
                _udpBroadcastClient = null;
            }
        });
    }

    public void StartListening()
    {
        if (_isListening) return;

        _isListening = true;
        _listenCts = new CancellationTokenSource();

        GD.Print("[LanDiscovery] Started listening on port ", BroadcastPort);

        Task.Run(async () =>
        {
            _udpListenClient = new UdpClient(BroadcastPort);
            _udpListenClient.EnableBroadcast = true;

            try
            {
                while (_isListening && !_listenCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult result = await _udpListenClient.ReceiveAsync();
                        string message = Encoding.UTF8.GetString(result.Buffer);

                        if (message.StartsWith(AnnounceMessage))
                        {
                            string[] parts = message.Split('|');
                            if (parts.Length >= 5)
                            {
                                string ip = parts[1];
                                int port = int.Parse(parts[2]);
                                string pin = parts[3];
                                string petLocation = parts[4];
                                GD.Print("[LanDiscovery] Host discovered: ", ip, ":", port, " Pet: ", petLocation);
                                CallDeferred(nameof(EmitHostDiscovered), ip, port, pin, petLocation);
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        if (!_listenCts.Token.IsCancellationRequested)
                            GD.PrintErr("[LanDiscovery] Listen error: ", e.Message);
                    }
                }
            }
            finally
            {
                _udpListenClient?.Close();
                _udpListenClient = null;
            }
        });
    }

    public void StopListening()
    {
        if (!_isListening) return;

        _isListening = false;
        _listenCts?.Cancel();
        _listenCts?.Dispose();
        _listenCts = null;

        try
        {
            _udpListenClient?.Close();
        }
        catch { }

        _udpListenClient = null;
        GD.Print("[LanDiscovery] Stopped listening");
    }

    public void StopBroadcasting()
    {
        if (!_isBroadcasting) return;

        _isBroadcasting = false;
        _broadcastCTS?.Cancel();
        _broadcastCTS?.Dispose();
        _broadcastCTS = null;

        try
        {
            _udpBroadcastClient?.Close();
        }
        catch { }

        _udpBroadcastClient = null;
        GD.Print("[LanDiscovery] Stopped broadcasting");
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string ipStr = ip.ToString();
                GD.Print("[LanDiscovery] Found local IP: ", ipStr);
                return ipStr;
            }
        }
        GD.PrintErr("[LanDiscovery] No local IP found, defaulting to 127.0.0.1");
        return "127.0.0.1";
    }

    private void EmitHostDiscovered(string ip, int port, string pin, string petLocation)
    {
        EmitSignal(SignalName.OnHostDiscovered, ip, port, pin, petLocation);
    }
}
