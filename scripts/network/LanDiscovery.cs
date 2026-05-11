using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
public partial class LanDiscovery : Node
{
[Signal] public delegate void OnHostDiscoveredEventHandler(string ip, int port, string pin, string petLocation);
private const int BroadcastPort = 18888;
private const int TcpPort = 18889;
private const string AnnounceMessage = "SLUMBER_PETS_ANNOUNCE";
private UdpClient _udpClient;
private bool _isBroadcasting = false;
public int DiscoveryTcpPort => TcpPort;
public override void _Ready()
{
GD.Print("[LanDiscovery] Initialized on port ", BroadcastPort);
}
public void StartBroadcasting(string pin, string petLocation)
{
if (_isBroadcasting) return;
_isBroadcasting = true;
Task.Run(async () =>
{
_udpClient = new UdpClient();
_udpClient.EnableBroadcast = true;
while (_isBroadcasting)
{
try
{
string localIp = GetLocalIPAddress();
string message = $"{AnnounceMessage}|{localIp}|{TcpPort}|{pin}|{petLocation}";
byte[] data = Encoding.UTF8.GetBytes(message);
IPEndPoint broadcastEp = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
await _udpClient.SendAsync(data, data.Length, broadcastEp);
await Task.Delay(2000);
}
catch (Exception e)
{
GD.PrintErr("[LanDiscovery] Broadcast error: ", e.Message);
await Task.Delay(2000);
}
}
});
GD.Print("[LanDiscovery] Started broadcasting");
}
public void StartListening()
{
Task.Run(async () =>
{
UdpClient listener = new UdpClient(BroadcastPort);
listener.EnableBroadcast = true;
while (true)
{
try
{
UdpReceiveResult result = await listener.ReceiveAsync();
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
CallDeferred(nameof(EmitHostDiscovered), ip, port, pin, petLocation);
}
}
}
catch (Exception e)
{
GD.PrintErr("[LanDiscovery] Listen error: ", e.Message);
}
}
});
GD.Print("[LanDiscovery] Started listening");
}
public void StopBroadcasting()
{
_isBroadcasting = false;
_udpClient?.Close();
GD.Print("[LanDiscovery] Stopped broadcasting");
}
private string GetLocalIPAddress()
{
var host = Dns.GetHostEntry(Dns.GetHostName());
foreach (var ip in host.AddressList)
{
if (ip.AddressFamily == AddressFamily.InterNetwork)
{
return ip.ToString();
}
}
return "127.0.0.1";
}
private void EmitHostDiscovered(string ip, int port, string pin, string petLocation)
{
EmitSignal(SignalName.OnHostDiscovered, ip, port, pin, petLocation);
}
}