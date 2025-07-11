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

        // TODO: 닉네임 중복 체크 로직 추가 필요

        // 로비에서만 닉네임 설정 가능
        S_SetNickname s_SetNickname = new S_SetNickname();
        if (clientSession.CurrentState != UserState.Lobby)
        {
            Console.WriteLine($"[C_SetNicknameHandler] User {clientSession.UserInfo.UserId} is not in Lobby state.");
            s_SetNickname.ErrorCode = ErrorCode.NotInLobby;
            s_SetNickname.Reason = "You must be in the Lobby to set a nickname.";
            clientSession.Send(s_SetNickname);
            return;
        }

        clientSession.UserInfo.Nickname = c_SetNicknamePacket.Nickname;
        s_SetNickname.ErrorCode = ErrorCode.Success;
        s_SetNickname.UserId = clientSession.UserInfo.UserId;
        s_SetNickname.Nickname = c_SetNicknamePacket.Nickname;
        RoomManager.Instance.BroadcastToLobby(s_SetNickname);
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
        int userId = clientSession.UserInfo.UserId;
        // 패킷 생성
        S_CreateRoom s_CreateRoom = new S_CreateRoom();

        if (clientSession.CurrentState != UserState.Lobby)
        {
            Console.WriteLine($"[C_CreateRoomHandler] User {clientSession.UserInfo.UserId} is not in Lobby state.");
            s_CreateRoom.ErrorCode = ErrorCode.NotInLobby;
            s_CreateRoom.Reason = "You must be in the Lobby to create a room.";
            clientSession.Send(s_CreateRoom);
        }
        else
        {
            Room room = RoomManager.Instance.CreateRoom(c_CreateRoomPacket.RoomName, userId);
            room.AddUser(clientSession);
            RoomInfo roomInfo = room.roomInfo;
            s_CreateRoom.RoomInfo = roomInfo;
            s_CreateRoom.ErrorCode = ErrorCode.Success;
            RoomManager.Instance.BroadcastToLobby(s_CreateRoom);

            RoomManager.Instance.LeaveUserFromLobby(userId); // 로비에서 유저 제거
            clientSession.CurrentState = UserState.Room;
            clientSession.Room = room; // 현재 방 정보 설정
        }
    }

    public static void C_DeleteRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_DeleteRoom c_DeleteRoomPacket = packet as C_DeleteRoom;

        // 패킷 생성
        S_DeleteRoom s_DeleteRoom = new S_DeleteRoom();
        Room clientSessionCurrentRoom = clientSession.Room;
        if (clientSessionCurrentRoom == null)
        {
            Console.WriteLine($"[C_DeleteRoomHandler] User {clientSession.UserInfo.UserId} is not in a room.");
            s_DeleteRoom.ErrorCode = ErrorCode.NotInRoom;
            s_DeleteRoom.Reason = "You are not in a room.";
            clientSession.Send(s_DeleteRoom);
            return;
        }

        foreach (var room in RoomManager.Instance.rooms.Values)
        {
            s_DeleteRoom.Rooms.Add(room.roomInfo.RoomId, room.roomInfo);
        }
        RoomManager.Instance.GetLobbyUsers(s_DeleteRoom.LobbyUserInfos); // 로비 유저 리스트 가져오기

        bool success = RoomManager.Instance.RemoveRoom(clientSessionCurrentRoom.roomInfo.RoomId, clientSession.UserInfo.UserId);
        s_DeleteRoom.ErrorCode = success ? ErrorCode.Success : ErrorCode.NotAuthorized;
        if (success)
        {
            clientSessionCurrentRoom.Broadcast(s_DeleteRoom);

            S_DeleteAnyRoomInLobby s_DeleteAnyRoomInLobby = new S_DeleteAnyRoomInLobby();
            s_DeleteAnyRoomInLobby.RoomId = clientSessionCurrentRoom.roomInfo.RoomId;

            // 방에 있는 모든 유저들을 로비로 이동
            lock (clientSessionCurrentRoom._lock)
            {
                foreach (var userInfo in clientSessionCurrentRoom.roomInfo.UserInfos.Values)
                {
                    SessionManager.Instance.clientSessions.TryGetValue(userInfo.UserId, out ClientSession cs);
                    // 유저 상태 변경
                    cs.CurrentState = UserState.Lobby;
                    cs.Room = null;
                    RoomManager.Instance.AddUserToLobby(userInfo.UserId);

                    s_DeleteAnyRoomInLobby.UserInfos.Add(userInfo);
                }
            }

            // 로비에 있는 유저들에게 삭제되는 방 정보와 로비에 추가될 유저 정보 전송
            RoomManager.Instance.BroadcastToLobby(s_DeleteAnyRoomInLobby);
        }
        else
        {
            Console.WriteLine($"[C_DeleteRoomHandler] User {clientSession.UserInfo.UserId} failed to delete room {clientSessionCurrentRoom.roomInfo.RoomId}.");
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
        // 유저 아이디 추출
        int userId = clientSession.UserInfo.UserId;

        Room room = RoomManager.Instance.GetRoom(c_EnterRoomPacket.RoomId);
        // 패킷 생성
        S_EnterRoom s_EnterRoom = new S_EnterRoom();
        if (room == null)
        {
            Console.WriteLine($"[C_EnterRoomHandler] Room {c_EnterRoomPacket.RoomId} not found.");
            s_EnterRoom.ErrorCode = ErrorCode.RoomNotFound;
            s_EnterRoom.Reason = "Room not found.";
            clientSession.Send(s_EnterRoom);
            return;
        }

        // 룸에 입장할 수 있는 상태인지 확인
        if (clientSession.CurrentState != UserState.Lobby)
        {
            Console.WriteLine($"[C_EnterRoomHandler] User {clientSession.UserInfo.UserId} is not in Lobby state.");
            s_EnterRoom.ErrorCode = ErrorCode.NotInLobby;
            s_EnterRoom.Reason = "You must be in the Lobby to enter a room.";
            clientSession.Send(s_EnterRoom);
            return;
        }

        // 룸에 유저 추가
        room.AddUser(clientSession);
        RoomManager.Instance.LeaveUserFromLobby(userId); // 로비에서 유저 제거
        clientSession.CurrentState = UserState.Room; // 현재 상태를 Room으로 변경
        clientSession.Room = room; // 현재 방 정보 설정
        s_EnterRoom.ErrorCode = ErrorCode.Success;
        s_EnterRoom.RoomInfo = room.roomInfo;

        // 룸 입장 응답 패킷 전송
        clientSession.Send(s_EnterRoom);

        // 룸에 있는 모든 유저에게 입장 알림 패킷 전송
        S_EnterRoomAnyUser s_EnterRoomAnyUser = new S_EnterRoomAnyUser();
        s_EnterRoomAnyUser.RoomId = room.roomInfo.RoomId;
        s_EnterRoomAnyUser.UserInfo = clientSession.UserInfo;
        room.Broadcast(s_EnterRoomAnyUser);

        // 로비에 있는 유저들에게 룸 입장 알림 패킷 전송
        RoomManager.Instance.BroadcastToLobby(s_EnterRoomAnyUser);
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
                Console.WriteLine($"[C_UserListHandler] User {userId} is not in a room or lobby.");
                return; // 현재 상태가 Lobby나 Room이 아닐 경우 처리하지 않음
            }
        }

        clientSession.Send(s_UserList);
    }

    public static void C_LeaveRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_LeaveRoom c_LeaveRoomPacket = packet as C_LeaveRoom;
        // 유저 아이디 추출
        int userId = clientSession.UserInfo.UserId;

        S_LeaveRoom s_LeaveRoom = new S_LeaveRoom();

        Room room = clientSession.Room; // 현재 방 정보
        if (clientSession.CurrentState != UserState.Room)
        {
            Console.WriteLine($"[C_LeaveRoomHandler] User {userId} is not in a room.");
            s_LeaveRoom.ErrorCode = ErrorCode.NotInRoom;
            s_LeaveRoom.UserState = clientSession.CurrentState;
            clientSession.Send(s_LeaveRoom);
            return; // 현재 상태가 Room이 아닐 경우 처리하지 않음
        }
        RoomManager.Instance.LeaveUserFromRoom(clientSession.Room.roomInfo.RoomId, userId);
        clientSession.CurrentState = UserState.Lobby; // 현재 상태를 Lobby로 변경
        clientSession.Room = null;

        // 퇴장하는 유저에게 방 목록과 로비 유저 리스트 전송
        foreach (var item in RoomManager.Instance.rooms)
        {
            s_LeaveRoom.Rooms.Add(item.Key, item.Value.roomInfo);
        }
        RoomManager.Instance.GetLobbyUsers(s_LeaveRoom.UserInfos); // 로비 유저 리스트 가져오기
        clientSession.Send(s_LeaveRoom);

        // 패킷 생성
        S_LeaveRoomAnyUser s_LeaveRoomAnyUser = new S_LeaveRoomAnyUser();
        s_LeaveRoomAnyUser.RoomId = room.roomInfo.RoomId;
        s_LeaveRoomAnyUser.UserInfo = clientSession.UserInfo;

        // 룸에 있는 모든 유저에게 퇴장 알림 패킷 전송
        room.Broadcast(s_LeaveRoomAnyUser);

        // 서버 로비에 퇴장하는 유저 추가
        RoomManager.Instance.AddUserToLobby(userId);

        // 로비에 있는 유저들에게 로비 입장 패킷 전송
        RoomManager.Instance.BroadcastToLobby(s_LeaveRoomAnyUser);
    }

    public static void C_EnterLobbyHandler(Session session, IMessage message)
    {
        ClientSession clientSession = session as ClientSession;
        C_EnterLobby c_EnterLobby = message as C_EnterLobby;

        // 현재 상태를 로비로 변경
        clientSession.CurrentState = UserState.Lobby;
        clientSession.Room = null; // 현재 방 정보 초기화

        // 패킷 생성
        S_EnterLobby s_EnterLobby = new S_EnterLobby();
        s_EnterLobby.UserInfo = clientSession.UserInfo;

        // 룸 리스트
        foreach (var room in RoomManager.Instance.rooms.Values)
        {
            s_EnterLobby.Rooms.Add(room.roomInfo.RoomId, room.roomInfo);
        }

        // 유저 리스트
        foreach (int userId in RoomManager.Instance.userIds)
        {
            ClientSession userSession = SessionManager.Instance.GetClientSession(userId);
            if (userSession != null)
            {
                s_EnterLobby.UserInfos.Add(userId, userSession.UserInfo);
            }
        }

        // 로비에 접속한 유저에게 로비 정보 전송
        clientSession.Send(s_EnterLobby);

        // 로비에 있는 유저 리스트에 추가
        RoomManager.Instance.AddUserToLobby(clientSession.UserInfo.UserId);

        // 로비에 있는 유저들에게 접속 알림 패킷 전송
        S_EnterLobbyAnyUser s_EnterLobbyAnyUser = new S_EnterLobbyAnyUser();
        s_EnterLobbyAnyUser.UserInfo = clientSession.UserInfo;
        RoomManager.Instance.BroadcastToLobby(s_EnterLobbyAnyUser);
    }

    public static void C_UserInfoHandler(Session session, IMessage message)
    {
        ClientSession clientSession = session as ClientSession;

        S_UserInfo s_UserInfo = new S_UserInfo();
        s_UserInfo.UserInfo = clientSession.UserInfo;
        s_UserInfo.UserState = clientSession.CurrentState;
        if (clientSession.CurrentState == UserState.Room)
            s_UserInfo.CurrentRoomId = clientSession.Room.roomInfo.RoomId;

        clientSession.Send(s_UserInfo);
    }
}