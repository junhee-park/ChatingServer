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
using Google.Protobuf.Collections;

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

        // 유저 아이디 추출
        int userId = clientSession.UserInfo.UserId;

        // 패킷 생성
        S_Chat s_Chat = new S_Chat();
        s_Chat.UserId = userId;
        s_Chat.Msg = c_ChatPacket.Msg;
        s_Chat.Timestamp = Timestamp.FromDateTime(DateTime.UtcNow);

        // 채팅 메시지를 룸에 있는 모든 유저에게 전송
        if (clientSession.CurrentState == UserState.Room && clientSession.Room != null)
        {
            clientSession.Room.Broadcast(s_Chat);
        }
        else
        {
            Console.WriteLine($"[C_ChatHandler] User {userId} is not in a room.");

        }
    }

    public static void C_PingHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Ping c_PingPacket = packet as C_Ping;

        clientSession.IsPing = false; // 핑 메시지를 받았음을 표시
    }

    public static void C_TestChatHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_TestChat c_TestChatPacket = packet as C_TestChat;
        // 유저 아이디 추출
        int userId = clientSession.UserInfo.UserId;
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

        RoomManager.Instance.Enqueue(RoomManager.Instance.SetNickname, clientSession, c_SetNicknamePacket);
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

        RoomManager.Instance.Enqueue(RoomManager.Instance.CreateRoom, clientSession, c_CreateRoomPacket);
    }

    public static void C_DeleteRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_DeleteRoom c_DeleteRoomPacket = packet as C_DeleteRoom;

        RoomManager.Instance.Enqueue(RoomManager.Instance.RemoveRoom, clientSession);
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
        int userId = clientSession.UserInfo.UserId;
        // 패킷 생성
        S_RoomList s_RoomList = new S_RoomList();
        // 현재 존재하는 룸리스트 반환
        foreach (Room room in RoomManager.Instance.rooms.Values)
        {
            s_RoomList.Rooms.Add(room.roomInfo.RoomId, room.roomInfo);
        }
        clientSession.Send(s_RoomList);
    }

    public static void C_EnterRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_EnterRoom c_EnterRoomPacket = packet as C_EnterRoom;

        RoomManager.Instance.Enqueue(RoomManager.Instance.EnterRoom, clientSession, c_EnterRoomPacket);
    }

    public static void C_UserListHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_UserList c_UserListPacket = packet as C_UserList;
        
        int userId = clientSession.UserInfo.UserId;
        // 패킷 생성
        S_UserList s_UserList = new S_UserList();

        // 로비에 있을 경우 로비 유저 리스트 전송
        if (clientSession.CurrentState == UserState.Lobby)
        {
            foreach (int id in RoomManager.Instance.userIds)
            {
                ClientSession userSession = SessionManager.Instance.clientSessions[id];
                UserInfo userInfo = userSession.UserInfo;
                s_UserList.RoomId = 0; // 로비에서는 RoomId가 0
                s_UserList.UserInfos.Add(id, userInfo);
            }
        }
        else
        {
            // 룸에 있을 경우 룸 유저 리스트 전송
            if (clientSession.CurrentState == UserState.Room && clientSession.Room != null)
            {
                s_UserList.RoomId = clientSession.Room.roomInfo.RoomId; // 현재 방의 ID 설정
                foreach (UserInfo userInfo in clientSession.Room.roomInfo.UserInfos.Values)
                {
                    s_UserList.UserInfos.Add(userInfo.UserId, userInfo);
                }
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow} [C_UserListHandler] User {userId} is not in a room or lobby.");
                return; // 현재 상태가 Lobby나 Room이 아닐 경우 처리하지 않음
            }
        }

        clientSession.Send(s_UserList);
    }

    public static void C_LeaveRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_LeaveRoom c_LeaveRoomPacket = packet as C_LeaveRoom;

        if (clientSession.CurrentState != UserState.Room)
        {
            S_LeaveRoom s_LeaveRoom = new S_LeaveRoom();
            Console.WriteLine($"{DateTime.UtcNow} [C_LeaveRoomHandler] User {clientSession.UserInfo.UserId} is not in a room.");
            s_LeaveRoom.ErrorCode = ErrorCode.NotInRoom;
            s_LeaveRoom.UserState = clientSession.CurrentState;
            clientSession.Send(s_LeaveRoom);
            return; // 현재 상태가 Room이 아닐 경우 처리하지 않음
        }
        else if (clientSession.Room.roomInfo.RoomMasterUserId == clientSession.UserInfo.UserId)
        {
            S_LeaveRoom s_LeaveRoom = new S_LeaveRoom();
            Console.WriteLine($"{DateTime.UtcNow} [C_LeaveRoomHandler] User {clientSession.UserInfo.UserId} is the room master and cannot leave the room.");
            s_LeaveRoom.ErrorCode = ErrorCode.RoomMasterCannotLeave;
            s_LeaveRoom.UserState = clientSession.CurrentState;
            clientSession.Send(s_LeaveRoom);
            return; // 방장인 경우 방을 나갈 수 없음
        }

        RoomManager.Instance.Enqueue(RoomManager.Instance.LeaveRoom, clientSession);
    }

    public static void C_EnterLobbyHandler(Session session, IMessage message)
    {
        ClientSession clientSession = session as ClientSession;
        C_EnterLobby c_EnterLobby = message as C_EnterLobby;

        RoomManager.Instance.Enqueue(RoomManager.Instance.EnterLobby, clientSession);

    }

    public static void C_UserInfoHandler(Session session, IMessage message)
    {
        ClientSession clientSession = session as ClientSession;

        S_UserInfo s_UserInfo = new S_UserInfo();
        s_UserInfo.UserInfo = clientSession.UserInfo;
        s_UserInfo.UserState = clientSession.CurrentState;
        if (clientSession.CurrentState == UserState.Room)
        {
            s_UserInfo.RoomInfo = new RoomInfo();
            s_UserInfo.RoomInfo.MergeFrom(clientSession.Room.roomInfo);
        }

        clientSession.Send(s_UserInfo);
    }
}