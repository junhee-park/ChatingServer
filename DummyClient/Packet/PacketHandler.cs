using Google.Protobuf;
using Google.Protobuf.Protocol;
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

        if (serverSession.CurrentState != UserState.Room)
            return;

        if (s_ChatPacket.ErrorCode != ErrorCode.Success)
        {
            // 채팅 실패 에러 출력
            Console.WriteLine(s_ChatPacket.Reason);
            serverSession.CurrentState = s_ChatPacket.UserState;
            return;
        }
    }

    /// <summary>
    /// 서버에서 전송된 채팅 메시지를 처리하는 핸들러.
    /// 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_ChatBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_ChatBc s_ChatBcPacket = packet as S_ChatBc;

        if (serverSession.CurrentState != UserState.Room)
            return;

        serverSession.ViewManager.ShowText($"{s_ChatBcPacket.Nickname}({s_ChatBcPacket.UserId})[{s_ChatBcPacket.Timestamp.ToDateTime()}]: {s_ChatBcPacket.Msg}");
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

    public static void S_SetNicknameHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_SetNickname s_SetNicknamePacket = packet as S_SetNickname;

        if (s_SetNicknamePacket.ErrorCode != ErrorCode.Success)
        {
            Console.WriteLine(s_SetNicknamePacket.Reason);
            serverSession.CurrentState = s_SetNicknamePacket.UserState; // 유저 상태 갱신
            return;
        }
    }

    /// <summary>
    /// 닉네임 변경 요청에 대한 응답 패킷을 처리하는 핸들러.
    /// 로비 유저 브로드캐스트.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_SetNicknameBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_SetNicknameBc s_SetNicknamePacket = packet as S_SetNicknameBc;

        // 없으면 다른 곳으로 이동했다는 의미이므로 갱신 안하고 본인일 경우 상관 없이 갱신 진행
        if (serverSession.UserInfo.UserId == s_SetNicknamePacket.UserId)
        {
            serverSession.UserInfo.Nickname = s_SetNicknamePacket.Nickname;
            serverSession.ViewManager.ShowChangedNickname(serverSession.UserInfo, s_SetNicknamePacket.Nickname);
        }
        else if (serverSession.RoomManager.UserInfos.TryGetValue(s_SetNicknamePacket.UserId, out UserInfo userInfo))
        {
            userInfo.Nickname = s_SetNicknamePacket.Nickname;
            serverSession.ViewManager.ShowChangedNickname(userInfo, s_SetNicknamePacket.Nickname);
        }
    }

    public static void S_CreateRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_CreateRoom s_CreateRoomPacket = packet as S_CreateRoom;

        if (s_CreateRoomPacket.ErrorCode != ErrorCode.Success)
        {
            Console.WriteLine(s_CreateRoomPacket.Reason);
            serverSession.CurrentState = s_CreateRoomPacket.UserState;
            return;
        }

        // 서버에서 할당한 방 아이디와 임시 방 이름, 방생성 요청한 유저 아이디를 현재 방 인포에 추가
        serverSession.RoomManager.CreateRoom(s_CreateRoomPacket.RoomInfo);

        // 로비 리스트에서 방을 생성한 유저 제거
        if (serverSession.RoomManager.UserInfos.TryGetValue(s_CreateRoomPacket.RoomInfo.RoomMasterUserId, out UserInfo roomMasterUser))
            serverSession.RoomManager.UserInfos.Remove(s_CreateRoomPacket.RoomInfo.RoomMasterUserId);

        // 방 생성자는 방에 입장한 것으로 간주하고 방 정보와 유저 리스트를 뷰 매니저에 전달하여 UI 갱신
        if (serverSession.UserInfo.UserId == s_CreateRoomPacket.RoomInfo.RoomMasterUserId)
        {
            serverSession.RoomManager.CurrentRoom = s_CreateRoomPacket.RoomInfo; // 현재 방 정보 설정
            // 방으로 스크린을 변경하고 방 유저 리스트를 보여줌
            serverSession.CurrentState = UserState.Room;
            serverSession.ViewManager.ShowRoomUserList(s_CreateRoomPacket.RoomInfo.UserInfos);
            serverSession.ViewManager.ShowText($"방 생성 성공: {s_CreateRoomPacket.RoomInfo.RoomName} (ID: {s_CreateRoomPacket.RoomInfo.RoomId})");
        }
        else
        {
            // 방 리스트에 방을 추가하고 방 리스트를 보여줌
            serverSession.ViewManager.ShowAddedRoom(s_CreateRoomPacket.RoomInfo);
            if (roomMasterUser != null)
                serverSession.ViewManager.ShowRemovedUser(0, roomMasterUser);
        }
    }

    /// <summary>
    /// 방 생성 요청에 대한 응답 패킷을 처리하는 핸들러.
    /// 로비 유저 대상 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_CreateRoomBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_CreateRoomBc s_CreateRoomPacket = packet as S_CreateRoomBc;

        // 서버에서 할당한 방 아이디와 임시 방 이름, 방생성 요청한 유저 아이디를 현재 방 인포에 추가
        serverSession.RoomManager.CreateRoom(s_CreateRoomPacket.RoomInfo);

        // 로비 리스트에서 방을 생성한 유저 제거
        if (serverSession.RoomManager.UserInfos.TryGetValue(s_CreateRoomPacket.RoomInfo.RoomMasterUserId, out UserInfo roomMasterUser))
            serverSession.RoomManager.UserInfos.Remove(s_CreateRoomPacket.RoomInfo.RoomMasterUserId);

        // 방 생성자는 방에 입장한 것으로 간주하고 방 정보와 유저 리스트를 뷰 매니저에 전달하여 UI 갱신
        if (serverSession.UserInfo.UserId == s_CreateRoomPacket.RoomInfo.RoomMasterUserId)
        {
            serverSession.RoomManager.CurrentRoom = s_CreateRoomPacket.RoomInfo; // 현재 방 정보 설정
            // 방으로 스크린을 변경하고 방 유저 리스트를 보여줌
            serverSession.CurrentState = UserState.Room;
            serverSession.ViewManager.ShowRoomUserList(s_CreateRoomPacket.RoomInfo.UserInfos);
            serverSession.ViewManager.ShowText($"방 생성 성공: {s_CreateRoomPacket.RoomInfo.RoomName} (ID: {s_CreateRoomPacket.RoomInfo.RoomId})");
        }
        else
        {
            // 방 리스트에 방을 추가하고 방 리스트를 보여줌
            serverSession.ViewManager.ShowAddedRoom(s_CreateRoomPacket.RoomInfo);
            if (roomMasterUser != null)
                serverSession.ViewManager.ShowRemovedUser(0, roomMasterUser);
        }
    }

    /// <summary>
    /// 방 삭제 요청에 대한 응답 패킷을 처리하는 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_DeleteRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_DeleteRoom s_DeleteRoomPacket = packet as S_DeleteRoom;

        if (s_DeleteRoomPacket.ErrorCode != ErrorCode.Success)
        {
            Console.WriteLine(s_DeleteRoomPacket.Reason);
            serverSession.CurrentState = s_DeleteRoomPacket.UserState; // 유저 상태 갱신
            return;
        }

        serverSession.CurrentState = UserState.Lobby;
    }

    /// <summary>
    /// 해당 방에 있는 유저 대상 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_DeleteRoomBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_DeleteRoomBc s_DeleteRoomPacket = packet as S_DeleteRoomBc;

        var roomManager = serverSession.RoomManager;
        // 방 삭제 로직
        roomManager.Refresh(s_DeleteRoomPacket.Rooms);
        roomManager.RefreshUserInfos(s_DeleteRoomPacket.LobbyUserInfos);

        // 방 목록과 유저 목록 정보를 뷰 매니저에 전달하여 UI 갱신
        serverSession.ViewManager.ShowRoomList(s_DeleteRoomPacket.Rooms);
        serverSession.ViewManager.ShowLobbyUserList(s_DeleteRoomPacket.LobbyUserInfos);
        serverSession.CurrentState = UserState.Lobby;
    }

    public static void S_RoomListHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_RoomList s_RoomListPacket = packet as S_RoomList;

        // 방 목록 갱신
        serverSession.RoomManager.Refresh(s_RoomListPacket.Rooms);
    }

    public static void S_EnterRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterRoom s_EnterRoomPacket = packet as S_EnterRoom;

        if (s_EnterRoomPacket.ErrorCode == ErrorCode.NotInLobby)
        {
            Console.WriteLine(s_EnterRoomPacket.Reason);
            serverSession.CurrentState = s_EnterRoomPacket.UserState; // 유저 상태 갱신
            return;
        }
        else if (s_EnterRoomPacket.ErrorCode == ErrorCode.RoomNotFound)
        {
            // 입장한 방이 삭제된 경우
            serverSession.ViewManager.ShowRemovedRoom(s_EnterRoomPacket.RoomInfo.RoomId);
            return;
        }
        else if (s_EnterRoomPacket.ErrorCode != ErrorCode.Success)
        {
            Console.WriteLine(s_EnterRoomPacket.Reason);
            return;
        }

        serverSession.RoomManager.CurrentRoom = s_EnterRoomPacket.RoomInfo; // 현재 방 정보 설정
        serverSession.CurrentState = UserState.Room;
        serverSession.ViewManager.ShowRoomUserList(serverSession.RoomManager.Rooms[s_EnterRoomPacket.RoomInfo.RoomId].UserInfos);
    }

    /// <summary>
    /// 같은 방 또는 로비에 있을 경우 누군가 방 입장했을 때 호출되는 핸들러.
    /// 해당 방에 있는 유저 대상 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_EnterRoomAnyUserBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterRoomAnyUserBc s_EnterRoomAnyUserPacket = packet as S_EnterRoomAnyUserBc;

        var roomManager = serverSession.RoomManager;

        roomManager.AddUserToRoom(s_EnterRoomAnyUserPacket.RoomId, s_EnterRoomAnyUserPacket.UserInfo);
        serverSession.ViewManager.ShowAddedUser(s_EnterRoomAnyUserPacket.RoomId, s_EnterRoomAnyUserPacket.UserInfo);
        serverSession.ViewManager.ShowRemovedUser(0, s_EnterRoomAnyUserPacket.UserInfo); // 로비에서 제거
    }

    /// <summary>
    /// 로비에 있을 경우 누군가 로비에 입장했을 때 호출되는 핸들러.
    /// 로비 유저 대상 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_EnterLobbyAnyUserBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterLobbyAnyUserBc s_EnterUserPacket = packet as S_EnterLobbyAnyUserBc;

        var roomManager = serverSession.RoomManager;

        // 로비에 유저가 입장했을 때
        roomManager.AddUserToLobby(s_EnterUserPacket.UserInfo);
        serverSession.ViewManager.ShowLobbyUserList(roomManager.UserInfos);
    }

    public static void S_UserListHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_UserList s_UserListPacket = packet as S_UserList;

        // 유저 리스트 갱신
        serverSession.RoomManager.RefreshUserInfos(s_UserListPacket.UserInfos);

        // 유저 리스트를 뷰 매니저에 전달하여 UI 갱신
        //serverSession.ViewManager.ShowUserList(s_UserListPacket.UserInfos);
    }

    /// <summary>
    /// 방에서 나갈 때 호출되는 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_LeaveRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveRoom s_LeaveRoomPacket = packet as S_LeaveRoom;

        if (s_LeaveRoomPacket.ErrorCode == ErrorCode.NotInRoom)
        {
            Console.WriteLine(s_LeaveRoomPacket.Reason);
            serverSession.CurrentState = s_LeaveRoomPacket.UserState; // 유저 상태 갱신
            return;
        }
        else if (s_LeaveRoomPacket.ErrorCode != ErrorCode.Success)
        {
            Console.WriteLine(s_LeaveRoomPacket.Reason);
            return;
        }

        serverSession.RoomManager.Refresh(s_LeaveRoomPacket.Rooms);
        serverSession.RoomManager.RefreshUserInfos(s_LeaveRoomPacket.UserInfos);

        // 방 목록과 유저 목록 정보를 뷰 매니저에 전달하여 UI 갱신
        serverSession.ViewManager.ShowRoomList(s_LeaveRoomPacket.Rooms);
        serverSession.ViewManager.ShowLobbyUserList(s_LeaveRoomPacket.UserInfos);
        serverSession.CurrentState = UserState.Lobby; // 로비 화면으로 전환
    }

    /// <summary>
    /// 로비에 입장했을 때 호출되는 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_EnterLobbyHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterLobby s_EnterLobby = packet as S_EnterLobby;

        // 룸 리스트 갱신
        serverSession.RoomManager.Refresh(s_EnterLobby.Rooms);

        // 로비에 입장했을 때 유저 리스트 갱신
        serverSession.RoomManager.RefreshUserInfos(s_EnterLobby.UserInfos);

        // 유저 인포를 세션에 캐싱 및 유저 리스트에 본인 추가
        serverSession.UserInfo = s_EnterLobby.UserInfo;
        serverSession.RoomManager.AddUserToLobby(s_EnterLobby.UserInfo);

        // 뷰 매니저에 룸 리스트와 유저 리스트를 전달하여 UI 갱신
        s_EnterLobby.UserInfos.TryAdd(s_EnterLobby.UserInfo.UserId, s_EnterLobby.UserInfo);
        serverSession.ViewManager.ShowRoomList(s_EnterLobby.Rooms);
        serverSession.ViewManager.ShowLobbyUserList(s_EnterLobby.UserInfos);
        serverSession.CurrentState = UserState.Lobby; // 로비 화면으로 전환
    }

    /// <summary>
    /// 방에서 유저가 나갔을 때 호출되는 핸들러.
    /// 해당 방에 있는 유저 대상 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_LeaveRoomAnyUserBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveRoomAnyUserBc s_LeaveRoomAnyUser = packet as S_LeaveRoomAnyUserBc;

        serverSession.RoomManager.LeaveRoom(s_LeaveRoomAnyUser.RoomId, s_LeaveRoomAnyUser.UserInfo);

        serverSession.ViewManager.ShowRemovedUser(s_LeaveRoomAnyUser.RoomId, s_LeaveRoomAnyUser.UserInfo);
        serverSession.ViewManager.ShowLobbyUserList(serverSession.RoomManager.UserInfos);
    }

    /// <summary>
    /// 로비에서 방이 삭제됬을 때 호출되는 핸들러.
    /// 로비 유저 대상 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_DeleteAnyRoomInLobbyBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_DeleteAnyRoomInLobbyBc s_DeleteAnyRoomInLobby = packet as S_DeleteAnyRoomInLobbyBc;

        // 방 삭제 로직
        serverSession.RoomManager.DeleteRoom(s_DeleteAnyRoomInLobby.RoomId);
        serverSession.ViewManager.ShowRemovedRoom(s_DeleteAnyRoomInLobby.RoomId);

        // 방에 있는 유저들을 방에서 제거하고 로비에 추가
        foreach (var userInfo in s_DeleteAnyRoomInLobby.UserInfos)
        {
            serverSession.RoomManager.AddUserToLobby(userInfo);
            serverSession.ViewManager.ShowAddedUser(0, userInfo);
        }
    }

    /// <summary>
    /// 로비에서 유저가 방으로 이동했을 때 호출되는 핸들러.
    /// 로비 유저 대상 브로드캐스트 패킷.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_LeaveLobbyAnyUserBcHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveLobbyAnyUserBc s_LeaveLobbyAnyUser = packet as S_LeaveLobbyAnyUserBc;

        serverSession.RoomManager.LeaveLobby(s_LeaveLobbyAnyUser.UserInfo);
        serverSession.ViewManager.ShowRemovedUser(0, s_LeaveLobbyAnyUser.UserInfo);

    }

    /// <summary>
    /// 유저 정보 패킷을 처리하는 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_UserInfoHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_UserInfo s_UserInfo = packet as S_UserInfo;

        serverSession.UserInfo = s_UserInfo.UserInfo;
        serverSession.CurrentState = s_UserInfo.UserState;
        if (serverSession.CurrentState == UserState.Room && serverSession.RoomManager.CurrentRoom == null)
        {
            serverSession.RoomManager.CurrentRoom = s_UserInfo.RoomInfo;
            serverSession.RoomManager.DeleteRoom(s_UserInfo.RoomInfo.RoomId);
            serverSession.RoomManager.CreateRoom(s_UserInfo.RoomInfo);
        }
    }
}