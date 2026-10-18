namespace Game.MonoGameClient;

// MonoGame delivery binding. Identity is supplied only by authenticated Steam sockets.
internal sealed class MonoConnection(int id, string? identity, Action<byte[], byte> send, Action close)
{
    public int Id { get; } = id;
    public string? Identity { get; } = identity;
    public void Send(byte[] bytes, byte channel) => send(bytes, channel);
    public void Close() => close();
}
