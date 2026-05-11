using Godot;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class SyncClient : Node
{
    [Signal] public delegate void OnConnectedEventHandler();
    [Signal] public delegate void OnDataReceivedEventHandler(string data);
    [Signal] public delegate void OnDisconnectedEventHandler();
    [Signal] public delegate void OnConnectionFailedEventHandler(string reason);

    private TcpClient _client;
    private bool _isConnected = false;
    private CancellationTokenSource _connectCTS;
    private Task _receiveTask;

    private const int ConnectTimeoutMs = 5000;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    public bool IsNetworkConnected => _isConnected;

    public override void _Ready()
    {
        GD.Print("[SyncClient] Initialized");
    }

    public override void _ExitTree()
    {
        Disconnect();
        _connectCTS?.Cancel();
        _connectCTS?.Dispose();
    }

    public void ConnectToHost(string ip, int port)
    {
        _connectCTS?.Cancel();
        _connectCTS?.Dispose();
        _connectCTS = new CancellationTokenSource();

        GD.Print("[SyncClient] Connecting to ", ip, ":", port, " (timeout: ", ConnectTimeoutMs, "ms, max retries: ", MaxRetries, ")");

        Task.Run(async () =>
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                if (_connectCTS.Token.IsCancellationRequested)
                {
                    GD.Print("[SyncClient] Connection cancelled");
                    return;
                }

                try
                {
                    _client = new TcpClient();
                    var connectTask = _client.ConnectAsync(ip, port);

                    if (await Task.WhenAny(connectTask, Task.Delay(ConnectTimeoutMs, _connectCTS.Token)) == connectTask)
                    {
                        await connectTask;
                    }
                    else
                    {
                        _client.Close();
                        _client = null;
                        GD.PrintErr("[SyncClient] Connection attempt ", attempt, "/", MaxRetries, " timed out");

                        if (attempt < MaxRetries)
                        {
                            GD.Print("[SyncClient] Retrying in ", RetryDelayMs, "ms...");
                            await Task.Delay(RetryDelayMs, _connectCTS.Token);
                        }
                        continue;
                    }

                    _isConnected = true;
                    CallDeferred(nameof(EmitConnected));
                    GD.Print("[SyncClient] Connected to ", ip, ":", port);

                    NetworkStream stream = _client.GetStream();
                    byte[] buffer = new byte[4096];

                    while (_isConnected && !_connectCTS.Token.IsCancellationRequested)
                    {
                        try
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _connectCTS.Token);
                            if (bytesRead == 0) break;
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            CallDeferred(nameof(EmitDataReceived), message);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch
                        {
                            break;
                        }
                    }

                    _isConnected = false;
                    CallDeferred(nameof(EmitDisconnected));
                    return;
                }
                catch (Exception e)
                {
                    _isConnected = false;
                    _client?.Close();
                    _client = null;

                    GD.PrintErr("[SyncClient] Connection attempt ", attempt, "/", MaxRetries, " failed: ", e.Message);

                    if (attempt < MaxRetries && !_connectCTS.Token.IsCancellationRequested)
                    {
                        GD.Print("[SyncClient] Retrying in ", RetryDelayMs, "ms...");
                        try
                        {
                            await Task.Delay(RetryDelayMs, _connectCTS.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            GD.Print("[SyncClient] Retry cancelled");
                            return;
                        }
                    }
                }
            }

            if (!_isConnected && !_connectCTS.Token.IsCancellationRequested)
            {
                CallDeferred(nameof(EmitConnectionFailed), "Connection failed after " + MaxRetries + " attempts");
                GD.PrintErr("[SyncClient] All connection attempts failed");
            }
        });
    }

    public void SendData(string data)
    {
        if (_client == null || !_client.Connected)
        {
            GD.PrintErr("[SyncClient] Cannot send data - not connected");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                GD.PrintErr("[SyncClient] Send error: ", e.Message);
            }
        });
    }

    public void Disconnect()
    {
        _connectCTS?.Cancel();
        _isConnected = false;

        try
        {
            _client?.Close();
        }
        catch { }

        _client = null;
        GD.Print("[SyncClient] Disconnected");
    }

    private void EmitConnected()
    {
        EmitSignal(SignalName.OnConnected);
    }

    private void EmitDataReceived(string data)
    {
        EmitSignal(SignalName.OnDataReceived, data);
    }

    private void EmitDisconnected()
    {
        EmitSignal(SignalName.OnDisconnected);
    }

    private void EmitConnectionFailed(string reason)
    {
        EmitSignal(SignalName.OnConnectionFailed, reason);
    }
}
