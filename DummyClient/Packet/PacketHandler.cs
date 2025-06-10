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
            serverSession.Nickname = serverSession.TempNickname;
            serverSession.UserId = serverSession.UserId;
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
        var roomManager = RoomManager.Instance;
        var roominfo = new RoomInfo();
        roominfo.RoomId = s_CreateRoomPacket.RoomId;
        roominfo.RoomName = roomManager.TempRoomName;
        roominfo.RoomMasterUserId = serverSession.UserId;
        roominfo.UserIds.Add(serverSession.UserId);
        roomManager.CurrentRoom = roominfo;
        roomManager.rooms.Add(roominfo.RoomId, roominfo);
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
        roomManager.rooms.Clear();
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
        // 서버에서 클라이언트로 방 입장 응답 패킷 생성
        C_EnterRoom c_EnterRoom = new C_EnterRoom();
        // TODO: 방 입장 로직 추가 필요
        // 클라이언트로 방 입장 응답 전송
        serverSession.Send(c_EnterRoom);
    }

    public static void S_UserListHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_UserList s_UserListPacket = packet as S_UserList;
        // 서버에서 클라이��트로 유저 목록 응답 패킷 생성
        C_UserList c_UserList = new C_UserList();
        // TODO: 유저 목록 생성 로직 추가 필요
        // 클라이언트로 유저 목록 응답 전송
        serverSession.Send(c_UserList);
    }

    public static void S_LeaveRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveRoom s_LeaveRoomPacket = packet as S_LeaveRoom;
        // 서버에서 클라이언트로 방 나가기 응답 패킷 생성
        C_LeaveRoom c_LeaveRoom = new C_LeaveRoom();
        // TODO: 방 나가기 로직 추가 필요
        // 클라이언트로 방 나가기 응답 전송
        serverSession.Send(c_LeaveRoom);
    }

}