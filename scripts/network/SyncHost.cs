using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
public partial class SyncHost : Node
{
[Signal] public delegate void OnClientConnectedEventHandler();
[Signal] public delegate void OnDataReceivedEventHandler(string data);
[Signal] public delegate void OnClientDisconnectedEventHandler();
private TcpListener _listener;
private TcpClient _connectedClient;
private bool _isRunning = false;
public override void _Ready()
{
GD.Print("[SyncHost] Initialized");
}
public void StartListening(int port)
{
if (_isRunning) return;
_isRunning = true;
Task.Run(async () =>
{
try
{
_listener = new TcpListener(IPAddress.Any, port);
_listener.Start();
GD.Print("[SyncHost] Listening on port ", port);
_connectedClient = await _listener.AcceptTcpClientAsync();
CallDeferred(nameof(EmitClientConnected));
NetworkStream stream = _connectedClient.GetStream();
byte[] buffer = new byte[4096];
while (_isRunning && _connectedClient.Connected)
{
try
{
int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
if (bytesRead == 0) break;
string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
CallDeferred(nameof(EmitDataReceived), message);
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
GD.PrintErr("[SyncHost] Error: ", e.Message);
}
});
}
public void SendData(string data)
{
if (_connectedClient == null || !_connectedClient.Connected) return;
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
_isRunning = false;
_connectedClient?.Close();
_listener?.Stop();
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