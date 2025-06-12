
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
        _handlers.Add((ushort)MsgId.SSetNickname, PacketHandler.S_SetNicknameHandler);
        _makePacket.Add((ushort)MsgId.SSetNickname, MakePacket<S_SetNickname>);
        _handlers.Add((ushort)MsgId.SCreateRoom, PacketHandler.S_CreateRoomHandler);
        _makePacket.Add((ushort)MsgId.SCreateRoom, MakePacket<S_CreateRoom>);
        _handlers.Add((ushort)MsgId.SDeleteRoom, PacketHandler.S_DeleteRoomHandler);
        _makePacket.Add((ushort)MsgId.SDeleteRoom, MakePacket<S_DeleteRoom>);
        _handlers.Add((ushort)MsgId.SRoomList, PacketHandler.S_RoomListHandler);
        _makePacket.Add((ushort)MsgId.SRoomList, MakePacket<S_RoomList>);
        _handlers.Add((ushort)MsgId.SEnterRoom, PacketHandler.S_EnterRoomHandler);
        _makePacket.Add((ushort)MsgId.SEnterRoom, MakePacket<S_EnterRoom>);
        _handlers.Add((ushort)MsgId.SUserList, PacketHandler.S_UserListHandler);
        _makePacket.Add((ushort)MsgId.SUserList, MakePacket<S_UserList>);
        _handlers.Add((ushort)MsgId.SLeaveRoom, PacketHandler.S_LeaveRoomHandler);
        _makePacket.Add((ushort)MsgId.SLeaveRoom, MakePacket<S_LeaveRoom>);
        _handlers.Add((ushort)MsgId.SEnterUser, PacketHandler.S_EnterUserHandler);
        _makePacket.Add((ushort)MsgId.SEnterUser, MakePacket<S_EnterUser>);
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
