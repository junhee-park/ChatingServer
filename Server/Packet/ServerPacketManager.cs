
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
        _handlers.Add((ushort)MsgId.CChat, PacketHandler.C_ChatHandler);
        _makePacket.Add((ushort)MsgId.CChat, MakePacket<C_Chat>);
        _handlers.Add((ushort)MsgId.CPing, PacketHandler.C_PingHandler);
        _makePacket.Add((ushort)MsgId.CPing, MakePacket<C_Ping>);
        _handlers.Add((ushort)MsgId.CSetNickname, PacketHandler.C_SetNicknameHandler);
        _makePacket.Add((ushort)MsgId.CSetNickname, MakePacket<C_SetNickname>);
        _handlers.Add((ushort)MsgId.CCreateRoom, PacketHandler.C_CreateRoomHandler);
        _makePacket.Add((ushort)MsgId.CCreateRoom, MakePacket<C_CreateRoom>);
        _handlers.Add((ushort)MsgId.CDeleteRoom, PacketHandler.C_DeleteRoomHandler);
        _makePacket.Add((ushort)MsgId.CDeleteRoom, MakePacket<C_DeleteRoom>);
        _handlers.Add((ushort)MsgId.CRoomList, PacketHandler.C_RoomListHandler);
        _makePacket.Add((ushort)MsgId.CRoomList, MakePacket<C_RoomList>);
        _handlers.Add((ushort)MsgId.CEnterRoom, PacketHandler.C_EnterRoomHandler);
        _makePacket.Add((ushort)MsgId.CEnterRoom, MakePacket<C_EnterRoom>);
        _handlers.Add((ushort)MsgId.CUserList, PacketHandler.C_UserListHandler);
        _makePacket.Add((ushort)MsgId.CUserList, MakePacket<C_UserList>);
        _handlers.Add((ushort)MsgId.CLeaveRoom, PacketHandler.C_LeaveRoomHandler);
        _makePacket.Add((ushort)MsgId.CLeaveRoom, MakePacket<C_LeaveRoom>);
        _handlers.Add((ushort)MsgId.CEnterLobby, PacketHandler.C_EnterLobbyHandler);
        _makePacket.Add((ushort)MsgId.CEnterLobby, MakePacket<C_EnterLobby>);
        _handlers.Add((ushort)MsgId.CTestChat, PacketHandler.C_TestChatHandler);
        _makePacket.Add((ushort)MsgId.CTestChat, MakePacket<C_TestChat>);

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
