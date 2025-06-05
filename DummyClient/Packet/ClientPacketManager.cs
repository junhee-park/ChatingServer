
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

public class PacketManager
{
    #region Singleton
    static PacketManager _instance = new PacketManager();
    public static PacketManager Instance { get { return _instance; } }
    #endregion

    Dictionary<ushort, Action<Session, IMessage>> _handlers = new Dictionary<ushort, Action<Session, IMessage>> ();
    Dictionary<ushort, Func<ArraySegment<byte>, IMessage>> _makePacket = new Dictionary<ushort, Func<ArraySegment<byte>, IMessage>>();
        
    public PacketManager()
    {
        _handlers.Add((ushort)MsgId.SChat, PacketHandler.S_ChatHandler);
        _makePacket.Add((ushort)MsgId.SChat, MakePacket<S_Chat>);
        _handlers.Add((ushort)MsgId.SPing, PacketHandler.S_PingHandler);
        _makePacket.Add((ushort)MsgId.SPing, MakePacket<S_Ping>);
        _handlers.Add((ushort)MsgId.STestChat, PacketHandler.S_TestChatHandler);
        _makePacket.Add((ushort)MsgId.STestChat, MakePacket<S_TestChat>);

    }

    public T MakePacket<T>(ArraySegment<byte> buffer) where T : IMessage, new()
    {
        T packet = new T();
        packet.MergeFrom(buffer);

        return packet;
    }

    public void InvokePacketHandler(Session session, ArraySegment<byte> buffer)
    {
        ushort size = BitConverter.ToUInt16(buffer.Array, 0);
        ushort packetId = BitConverter.ToUInt16(buffer.Array, 2);

        ArraySegment<byte> data = new ArraySegment<byte>(buffer.Array, 4, size - 4);
        bool result = _makePacket.TryGetValue(packetId, out var makePacketFunc);
        if (!result)
        {
            return;
        }
        IMessage packet = makePacketFunc.Invoke(data);

        result = _handlers.TryGetValue(packetId, out var handler);
        if (!result)
        {
            return;
        }
        handler?.Invoke(session, packet);
    }
}
