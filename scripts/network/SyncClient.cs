using Godot;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
public partial class SyncClient : Node
{
[Signal] public delegate void OnConnectedEventHandler();
[Signal] public delegate void OnDataReceivedEventHandler(string data);
[Signal] public delegate void OnDisconnectedEventHandler();
[Signal] public delegate void OnConnectionFailedEventHandler(string reason);
private TcpClient _client;
private bool _isConnected = false;
public bool IsNetworkConnected => _isConnected;
public override void _Ready()
{
GD.Print("[SyncClient] Initialized");
}
public void ConnectToHost(string ip, int port)
{
Task.Run(async () =>
{
try
{
_client = new TcpClient();
await _client.ConnectAsync(ip, port);
_isConnected = true;
CallDeferred(nameof(EmitConnected));
GD.Print("[SyncClient] Connected to ", ip, ":", port);
NetworkStream stream = _client.GetStream();
byte[] buffer = new byte[4096];
while (_isConnected)
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
_isConnected = false;
CallDeferred(nameof(EmitDisconnected));
}
catch (Exception e)
{
_isConnected = false;
CallDeferred(nameof(EmitConnectionFailed), e.Message);
GD.PrintErr("[SyncClient] Connection failed: ", e.Message);
}
});
}
public void SendData(string data)
{
if (_client == null || !_client.Connected) return;
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
_isConnected = false;
_client?.Close();
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