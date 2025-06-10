using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using ServerCore;
using Google.Protobuf.Protocol;
using Server;
using Google.Protobuf.WellKnownTypes;

/// <summary>
/// 전송된 패킷을 처리하는 클래스.
/// 패킷 이름 + Handler 규칙으로 핸들러 함수를 작성해야 해당 패킷 받았을 때 핸들러가 실행됨.
/// </summary>
public static class PacketHandler
{
    public static void C_ChatHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Chat c_ChatPacket = packet as C_Chat;

        //C_Chat c_Chat = new C_Chat();
        //c_Chat.Read(buffer);

        // 유저 아이디 추출
        int userId = clientSession.UserId;

        // 패킷 생성
        S_Chat s_Chat = new S_Chat();
        s_Chat.UserId = userId;
        s_Chat.Msg = c_ChatPacket.Msg;
        s_Chat.Timestamp = Timestamp.FromDateTime(DateTime.UtcNow);

        SessionManager.Instance.Boardcast(s_Chat);
    }

    public static void C_PingHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Ping c_PingPacket = packet as C_Ping;
        // Ping 응답 패킷 생성
        S_Ping s_Ping = new S_Ping();
        // Ping 응답 전송
        clientSession.Send(s_Ping);
    }

    public static void C_TestChatHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_TestChat c_TestChatPacket = packet as C_TestChat;
        // 유저 아이디 추출
        int userId = clientSession.UserId;
        // 패킷 생성
        S_TestChat s_TestChat = new S_TestChat();
        S_Chat s_chat = new S_Chat();
        s_chat.UserId = userId;
        s_chat.Msg = c_TestChatPacket.Chat.Msg;
        s_TestChat.Chat = s_chat;
        s_TestChat.TickCount = c_TestChatPacket.TickCount;
        SessionManager.Instance.Boardcast(s_TestChat);
    }

    public static void C_SetNicknameHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_SetNickname c_SetNicknamePacket = packet as C_SetNickname;

        // TODO: 닉네임 중복 체크 로직 추가 필요

        clientSession.Nickname = c_SetNicknamePacket.Nickname;
        // 패킷 생성
        S_SetNickname s_SetNickname = new S_SetNickname();
        s_SetNickname.Success = true;

        clientSession.Send(s_SetNickname);
    }


    /// <summary>
    /// 방 생성 완료 시 생성 요청한 유저에게 방 생성 응답 패킷을 전송하는 핸들러.
    /// 생성한 유저는 해당 방에 자동으로 입장하게 됨.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void C_CreateRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_CreateRoom c_CreateRoomPacket = packet as C_CreateRoom;

        // 유저 아이디 추출
        int userId = clientSession.UserId;
        // 패킷 생성
        S_CreateRoom s_CreateRoom = new S_CreateRoom();

        if (clientSession.CurrentState != State.Lobby)
        {
            Console.WriteLine($"[C_CreateRoomHandler] User {clientSession.UserId} is not in Lobby state.");
            s_CreateRoom.Success = false;
            s_CreateRoom.Reason = "You must be in the Lobby to create a room.";
        }
        else
        {
            Room room = RoomManager.Instance.CreateRoom(c_CreateRoomPacket.RoomName, userId);
            room.AddUser(clientSession);
            s_CreateRoom.RoomId = room.roomInfo.RoomId;
            s_CreateRoom.Success = true;
            clientSession.CurrentState = State.Room;
            clientSession.Room = room; // 현재 방 정보 설정
        }
        clientSession.Send(s_CreateRoom);
    }

    public static void C_DeleteRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_DeleteRoom c_DeleteRoomPacket = packet as C_DeleteRoom;

        // 패킷 생성
        S_DeleteRoom s_DeleteRoom = new S_DeleteRoom();
        RoomInfo roomInfo = clientSession.Room.roomInfo;

        bool success = RoomManager.Instance.RemoveRoom(roomInfo.RoomId, clientSession.UserId);
        s_DeleteRoom.Success = success;
        if (success)
        {
            foreach(var room in RoomManager.Instance.rooms.Values)
            {
                s_DeleteRoom.Rooms.Add(room.roomInfo);
            }
            clientSession.Room.Broadcast(s_DeleteRoom);
            clientSession.CurrentState = State.Lobby; // 방 삭제 후 Lobby 상태로 변경
        }
        else
        {
            Console.WriteLine($"[C_DeleteRoomHandler] User {clientSession.UserId} failed to delete room {roomInfo.RoomId}.");
            s_DeleteRoom.Reason = "Failed to delete room. You must be the room master.";
            clientSession.Send(s_DeleteRoom);
        }
    }

    /// <summary>
    /// 룸 리스트 요청 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void C_RoomListHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_RoomList c_RoomListPacket = packet as C_RoomList;
        // 유저 아이디 추출
        int userId = clientSession.UserId;
        // 패킷 생성
        S_RoomList s_RoomList = new S_RoomList();
        // 현재 존재하는 룸리스트 반환
        foreach (Room room in RoomManager.Instance.rooms.Values)
        {
            s_RoomList.Rooms.Add(room.roomInfo);
        }
        clientSession.Send(s_RoomList);
    }

    public static void C_EnterRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_EnterRoom c_EnterRoomPacket = packet as C_EnterRoom;
        // 유저 아이디 추출
        int userId = clientSession.UserId;
        // 패킷 생성
        S_EnterRoom s_EnterRoom = new S_EnterRoom();
        // TODO: 룸 입장
        SessionManager.Instance.Boardcast(s_EnterRoom);
    }

    public static void C_UserListHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_UserList c_UserListPacket = packet as C_UserList;
        
        int userId = clientSession.UserId;
        // 패킷 생성
        S_UserList s_UserList = new S_UserList();
        // TODO: 현재 룸의 유저 리스트 전송
        SessionManager.Instance.Boardcast(s_UserList);
    }

    public static void C_LeaveRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_LeaveRoom c_LeaveRoomPacket = packet as C_LeaveRoom;
        // 유저 아이디 추출
        int userId = clientSession.UserId;
        // 패킷 생성
        S_LeaveRoom s_LeaveRoom = new S_LeaveRoom();
        // TODO: 방 퇴장
        SessionManager.Instance.Boardcast(s_LeaveRoom);
    }


}