using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DummyClient;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using ServerCore;

/// <summary>
/// 전송된 패킷을 처리하는 클래스.
/// 패킷 이름 + Handler 규칙으로 핸들러 함수를 작성해야 해당 패킷 받았을 때 핸들러가 실행됨.
/// </summary>
public static class PacketHandler
{
    public static void S_ChatHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_Chat s_ChatPacket = packet as S_Chat;

        serverSession.ViewManager.ShowText($"{s_ChatPacket.UserId}[{s_ChatPacket.Timestamp.ToDateTime()}]: {s_ChatPacket.Msg}");
    }

    public static void S_PingHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_Ping s_PingPacket = packet as S_Ping;
        // Ping 응답 패킷 생성
        C_Ping c_Ping = new C_Ping();
        // Ping 응답 전송
        serverSession.Send(c_Ping);
    }

    public static void S_TestChatHandler(Session session, IMessage packet)
    {
        TestServerSession testSession = session as TestServerSession;
        S_TestChat s_TestChatPacket = packet as S_TestChat;

        testSession?.TestCompareRtt(s_TestChatPacket);
    }

   public static void S_SetNicknameHandler(Session session, IMessage packet)
   {
        ServerSession serverSession = session as ServerSession;
        S_SetNickname s_SetNicknamePacket = packet as S_SetNickname;

        if (s_SetNicknamePacket.Success)
        {
            serverSession.UserInfo.Nickname = serverSession.TempNickname;
            serverSession.UserInfo.UserId = s_SetNicknamePacket.UserId;
        }
    }

    public static void S_CreateRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_CreateRoom s_CreateRoomPacket = packet as S_CreateRoom;

        if (!s_CreateRoomPacket.Success)
        {
            Console.WriteLine(s_CreateRoomPacket.Reason);
            return;
        }

        // 서버에서 할당한 방 아이디와 임시 방 이름, 방생성 요청한 유저 아이디를 현재 방 인포에 추가
        RoomManager.Instance.CreateRoom(s_CreateRoomPacket.RoomId, serverSession.UserInfo.UserId);
    }

    public static void S_DeleteRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_DeleteRoom s_DeleteRoomPacket = packet as S_DeleteRoom;

        if (!s_DeleteRoomPacket.Success)
        {
            Console.WriteLine(s_DeleteRoomPacket.Reason);
            return;
        }
        var roomManager = RoomManager.Instance;
        // 방 삭제 로직
        roomManager.Refresh(s_DeleteRoomPacket.Rooms);
        roomManager.CurrentRoom = null; // 현재 방 정보 초기화
    }

    public static void S_RoomListHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_RoomList s_RoomListPacket = packet as S_RoomList;

        // 방 목록 갱신
        RoomManager.Instance.Refresh(s_RoomListPacket.Rooms);
    }

    public static void S_EnterRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterRoom s_EnterRoomPacket = packet as S_EnterRoom;

        if (!s_EnterRoomPacket.Success)
        {
            Console.WriteLine(s_EnterRoomPacket.Reason);
            return;
        }
        RoomManager.Instance.AddUserToRoom(s_EnterRoomPacket.RoomInfo.RoomId, serverSession.UserInfo);

    }

    /// <summary>
    /// 방 또는 로비에 있을 경우 누군가 어떤 방 입장했을 때 호출되는 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_EnterUserHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterUser s_EnterUserPacket = packet as S_EnterUser;

        var roomManager = RoomManager.Instance;
        roomManager.AddUserToRoom(s_EnterUserPacket.RoomId, serverSession.UserInfo);
    }

    public static void S_UserListHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_UserList s_UserListPacket = packet as S_UserList;

        // TODO: 유저 리스트 갱신
    }

    public static void S_LeaveRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveRoom s_LeaveRoomPacket = packet as S_LeaveRoom;

        RoomManager.Instance.LeaveRoom(serverSession.UserInfo);
    }

}