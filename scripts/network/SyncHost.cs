using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class SyncHost : Node
{
    [Signal] public delegate void OnClientConnectedEventHandler();
    [Signal] public delegate void OnDataReceivedEventHandler(string data);
    [Signal] public delegate void OnClientDisconnectedEventHandler();

    private TcpListener _listener;
    private TcpClient _connectedClient;
    private bool _isRunning = false;
    private CancellationTokenSource _listenCTS;
    private Task _listenTask;

    public override void _Ready()
    {
        GD.Print("[SyncHost] Initialized");
    }

    public override void _ExitTree()
    {
        Stop();
    }

    public void StartListening(int port)
    {
        if (_isRunning) return;

        _isRunning = true;
        _listenCTS = new CancellationTokenSource();

        GD.Print("[SyncHost] Starting listener on port ", port);

        _listenTask = Task.Run(async () =>
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                GD.Print("[SyncHost] Listening on port ", port);

                var acceptTask = _listener.AcceptTcpClientAsync();
                var cancelTask = Task.Delay(Timeout.Infinite, _listenCTS.Token);

                if (await Task.WhenAny(acceptTask, cancelTask) == cancelTask)
                {
                    GD.Print("[SyncHost] Listen cancelled");
                    return;
                }

                _connectedClient = await acceptTask;
                CallDeferred(nameof(EmitClientConnected));
                GD.Print("[SyncHost] Client connected from ", _connectedClient.Client.RemoteEndPoint);

                NetworkStream stream = _connectedClient.GetStream();
                byte[] buffer = new byte[4096];

                while (_isRunning && _connectedClient.Connected && !_listenCTS.Token.IsCancellationRequested)
                {
                    try
                    {
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length, _listenCTS.Token);
                        var timeoutTask = Task.Delay(Timeout.Infinite, _listenCTS.Token);

                        if (await Task.WhenAny(readTask, timeoutTask) == timeoutTask)
                        {
                            break;
                        }

                        int bytesRead = await readTask;
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

                CallDeferred(nameof(EmitClientDisconnected));
            }
            catch (Exception e)
            {
                if (!_listenCTS.Token.IsCancellationRequested)
                    GD.PrintErr("[SyncHost] Error: ", e.Message);
            }
            finally
            {
                _listener?.Stop();
                _listener = null;
            }
        });
    }

    public void SendData(string data)
    {
        if (_connectedClient == null || !_connectedClient.Connected)
        {
            GD.PrintErr("[SyncHost] Cannot send data - no client connected");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                NetworkStream stream = _connectedClient.GetStream();
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                GD.PrintErr("[SyncHost] Send error: ", e.Message);
            }
        });
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _listenCTS?.Cancel();
        _listenCTS?.Dispose();
        _listenCTS = null;

        try
        {
            _connectedClient?.Close();
        }
        catch { }
        _connectedClient = null;

        try
        {
            _listener?.Stop();
        }
        catch { }
        _listener = null;

        GD.Print("[SyncHost] Stopped");
    }

    private void EmitClientConnected()
    {
        EmitSignal(SignalName.OnClientConnected);
    }

    private void EmitDataReceived(string data)
    {
        EmitSignal(SignalName.OnDataReceived, data);
    }

    private void EmitClientDisconnected()
    {
        EmitSignal(SignalName.OnClientDisconnected);
    }
}
