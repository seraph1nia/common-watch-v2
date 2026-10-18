using System.Runtime.InteropServices;
using Steamworks;
using Steamworks.Data;

namespace Game.MonoGameClient;

// Steam Networking Sockets owns authentication, reliability and relay routing.
internal sealed class MonoSteamTransport(Action<MonoConnection> connected, Action<MonoConnection, byte[]> received,
    Action<MonoConnection> disconnected) : IDisposable
{
    private HostSocket? _socket;
    private GuestSocket? _guest;
    private readonly Dictionary<uint, MonoConnection> _connections = [];
    private int _next;
    private bool _disposed;
    private ulong _expectedHost;
    private Func<ulong, bool> _member = _ => true;
    public void Host(Func<ulong, bool> member)
    { _member = member; _socket = SteamNetworkingSockets.CreateRelaySocket<HostSocket>(); _socket.Owner = this; }
    public void Join(ulong host)
    { _expectedHost = host; _guest = SteamNetworkingSockets.ConnectRelay<GuestSocket>(host); _guest.Owner = this; }
    private void Connected(Connection connection, ConnectionInfo info)
    {
        if (_disposed || info.Identity.SteamId.Value == 0 || _expectedHost != 0 && info.Identity.SteamId.Value != _expectedHost)
        { connection.Close(); return; }
        var peer = new MonoConnection(++_next, info.Identity.SteamId.Value.ToString(), (bytes, channel) =>
        {
            if (connection.SendMessage(bytes, SendType.Reliable) != Result.OK) throw new IOException("Steam reliable send failed.");
        }, () => connection.Close());
        _connections[connection.Id] = peer; connected(peer);
    }
    private void Disconnected(Connection connection)
    { if (_connections.Remove(connection.Id, out var peer) && !_disposed) disconnected(peer); connection.Close(); }
    private void Message(Connection connection, IntPtr pointer, int size)
    {
        if (_disposed || !_connections.TryGetValue(connection.Id, out var peer)) return;
        if (size is <= 0 or > MonoSession.MaximumMessageBytes) { peer.Close(); return; }
        byte[] data = new byte[size]; Marshal.Copy(pointer, data, 0, size); received(peer, data);
    }
    public void Poll() { if (_disposed) return; _socket?.Receive(32, receiveToEnd: false); _guest?.Receive(32, receiveToEnd: false); }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        foreach (var peer in _connections.Values) peer.Close();
        _connections.Clear(); _guest?.Close(linger: true); _socket?.Close();
        // Facepunch retains manager registrations; their disposed routers must
        // release application callbacks and lobby closures immediately.
        connected = static _ => { };
        received = static (_, _) => { };
        disconnected = static _ => { };
        _member = static _ => false;
    }
    public sealed class HostSocket : SocketManager
    {
        internal MonoSteamTransport Owner = null!;
        public override void OnConnecting(Connection connection, ConnectionInfo info)
        { if (Owner._disposed || info.Identity.SteamId.Value == 0 || !Owner._member(info.Identity.SteamId.Value) || Connecting.Count + Connected.Count > 8) connection.Close(); else connection.Accept(); }
        public override void OnConnected(Connection connection, ConnectionInfo info) { base.OnConnected(connection, info); Owner.Connected(connection, info); }
        public override void OnDisconnected(Connection connection, ConnectionInfo info) => Owner.Disconnected(connection);
        public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
            => Owner.Message(connection, data, size);
    }
    public sealed class GuestSocket : ConnectionManager
    {
        internal MonoSteamTransport Owner = null!;
        public override void OnConnected(ConnectionInfo info) => Owner.Connected(Connection, info);
        public override void OnDisconnected(ConnectionInfo info) => Owner.Disconnected(Connection);
        public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) => Owner.Message(Connection, data, size);
    }
}
