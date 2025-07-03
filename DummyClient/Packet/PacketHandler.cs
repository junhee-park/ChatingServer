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
using Google.Protobuf.Collections;
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

        string nickname = string.Empty;
        if (RoomManager.Instance.CurrentRoom != null && RoomManager.Instance.CurrentRoom.UserInfos.TryGetValue(s_ChatPacket.UserId, out UserInfo userInfo))
            nickname = userInfo.Nickname;

        serverSession.ViewManager.ShowText($"{nickname} {s_ChatPacket.UserId}[{s_ChatPacket.Timestamp.ToDateTime()}]: {s_ChatPacket.Msg}");
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

        // 닉네임 변경 실패 에러 출력
        if (s_SetNicknamePacket.Success == false)
        {
            Console.WriteLine(s_SetNicknamePacket.Reason);
            return;
        }

        UserInfo userInfo = RoomManager.Instance.UserInfos[s_SetNicknamePacket.UserId];
        userInfo.Nickname = s_SetNicknamePacket.Nickname;

        serverSession.ViewManager.ShowChangedNickname(userInfo, s_SetNicknamePacket.Nickname);
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
        RoomManager.Instance.CreateRoom(s_CreateRoomPacket.RoomInfo);

        // 로비 리스트에서 방을 생성한 유저 제거
        var roomMasterUser = RoomManager.Instance.UserInfos[s_CreateRoomPacket.RoomInfo.RoomMasterUserId];
        RoomManager.Instance.UserInfos.Remove(s_CreateRoomPacket.RoomInfo.RoomMasterUserId);

        // 방 생성자는 방에 입장한 것으로 간주하고 방 정보와 유저 리스트를 뷰 매니저에 전달하여 UI 갱신
        if (serverSession.UserInfo.UserId == s_CreateRoomPacket.RoomInfo.RoomMasterUserId)
        {
            RoomManager.Instance.CurrentRoom = s_CreateRoomPacket.RoomInfo; // 현재 방 정보 설정
            // 방으로 스크린을 변경하고 방 유저 리스트를 보여줌
            serverSession.ViewManager.ShowRoomScreen();
            serverSession.ViewManager.ShowRoomUserList(s_CreateRoomPacket.RoomInfo.UserInfos);
            serverSession.ViewManager.ShowText($"방 생성 성공: {s_CreateRoomPacket.RoomInfo.RoomName} (ID: {s_CreateRoomPacket.RoomInfo.RoomId})");
        }
        else
        {
            // 방 리스트에 방을 추가하고 방 리스트를 보여줌
            serverSession.ViewManager.ShowAddedRoom(s_CreateRoomPacket.RoomInfo);
            serverSession.ViewManager.ShowRemovedUser(0, roomMasterUser);
        }
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
        roomManager.RefreshUserInfos(s_DeleteRoomPacket.LobbyUserInfos);
        roomManager.CurrentRoom = null; // 현재 방 정보 초기화

        // 방 목록과 유저 목록 정보를 뷰 매니저에 전달하여 UI 갱신
        serverSession.ViewManager.ShowRoomList(s_DeleteRoomPacket.Rooms);
        serverSession.ViewManager.ShowLobbyUserList(s_DeleteRoomPacket.LobbyUserInfos);
        serverSession.ViewManager.ShowLobbyScreen(); // 로비 화면으로 전환
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

        RoomManager.Instance.CurrentRoom = s_EnterRoomPacket.RoomInfo; // 현재 방 정보 설정
        serverSession.ViewManager.ShowRoomScreen();
        serverSession.ViewManager.ShowRoomUserList(RoomManager.Instance.Rooms[s_EnterRoomPacket.RoomInfo.RoomId].UserInfos);
    }

    /// <summary>
    /// 같은 방 또는 로비에 있을 경우 누군가 방 입장했을 때 호출되는 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_EnterRoomAnyUserHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterRoomAnyUser s_EnterRoomAnyUserPacket = packet as S_EnterRoomAnyUser;

        var roomManager = RoomManager.Instance;

        roomManager.AddUserToRoom(s_EnterRoomAnyUserPacket.RoomId, s_EnterRoomAnyUserPacket.UserInfo);
        serverSession.ViewManager.ShowAddedUser(s_EnterRoomAnyUserPacket.RoomId, s_EnterRoomAnyUserPacket.UserInfo);
        serverSession.ViewManager.ShowRemovedUser(0, s_EnterRoomAnyUserPacket.UserInfo); // 로비에서 제거
    }

    /// <summary>
    /// 로비에 있을 경우 누군가 로비에 입장했을 때 호출되는 핸들러.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="packet"></param>
    public static void S_EnterLobbyAnyUserHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterLobbyAnyUser s_EnterUserPacket = packet as S_EnterLobbyAnyUser;

        var roomManager = RoomManager.Instance;

        // 로비에 유저가 입장했을 때
        roomManager.AddUserToLobby(s_EnterUserPacket.UserInfo);
        serverSession.ViewManager.ShowLobbyUserList(roomManager.UserInfos);
    }

    public static void S_UserListHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_UserList s_UserListPacket = packet as S_UserList;

        // 유저 리스트 갱신
        RoomManager.Instance.RefreshUserInfos(s_UserListPacket.UserInfos);

        // 유저 리스트를 뷰 매니저에 전달하여 UI 갱신
        //serverSession.ViewManager.ShowUserList(s_UserListPacket.UserInfos);
    }

    public static void S_LeaveRoomHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveRoom s_LeaveRoomPacket = packet as S_LeaveRoom;

        RoomManager.Instance.Refresh(s_LeaveRoomPacket.Rooms);
        RoomManager.Instance.RefreshUserInfos(s_LeaveRoomPacket.UserInfos);
        RoomManager.Instance.CurrentRoom = null; // 현재 방 정보 초기화

        // 방 목록과 유저 목록 정보를 뷰 매니저에 전달하여 UI 갱신
        serverSession.ViewManager.ShowRoomList(s_LeaveRoomPacket.Rooms);
        serverSession.ViewManager.ShowLobbyUserList(s_LeaveRoomPacket.UserInfos);
        serverSession.ViewManager.ShowLobbyScreen(); // 로비 화면으로 전환
    }

    public static void S_EnterLobbyHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_EnterLobby s_EnterLobby = packet as S_EnterLobby;

        // 룸 리스트 갱신
        RoomManager.Instance.Refresh(s_EnterLobby.Rooms);

        // 로비에 입장했을 때 유저 리스트 갱신
        RoomManager.Instance.RefreshUserInfos(s_EnterLobby.UserInfos);

        // 유저 인포를 세션에 캐싱
        serverSession.UserInfo = s_EnterLobby.UserInfo;

        // 뷰 매니저에 룸 리스트와 유저 리스트를 전달하여 UI 갱신
        serverSession.ViewManager.ShowRoomList(s_EnterLobby.Rooms);
        serverSession.ViewManager.ShowLobbyUserList(s_EnterLobby.UserInfos);

    }

    public static void S_LeaveRoomAnyUserHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveRoomAnyUser s_LeaveRoomAnyUser = packet as S_LeaveRoomAnyUser;

        RoomManager.Instance.LeaveRoom(s_LeaveRoomAnyUser.RoomId, s_LeaveRoomAnyUser.UserInfo);

        serverSession.ViewManager.ShowRemovedUser(s_LeaveRoomAnyUser.RoomId, s_LeaveRoomAnyUser.UserInfo);
        serverSession.ViewManager.ShowLobbyUserList(RoomManager.Instance.UserInfos);
    }

    public static void S_DeleteRoomInLobbyHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_DeleteRoomInLobby s_DeleteRoomInLobby = packet as S_DeleteRoomInLobby;

        // 방 삭제 로직
        RoomManager.Instance.DeleteRoom(s_DeleteRoomInLobby.RoomId);
        serverSession.ViewManager.ShowRemovedRoom(s_DeleteRoomInLobby.RoomId);

        // 방에 있는 유저들을 방에서 제거하고 로비에 추가
        foreach (var userInfo in s_DeleteRoomInLobby.UserInfos)
        {
            RoomManager.Instance.AddUserToLobby(userInfo);
            serverSession.ViewManager.ShowAddedUser(0, userInfo);
        }
    }

    public static void S_LeaveLobbyAnyUserHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_LeaveLobbyAnyUser s_LeaveLobbyAnyUser = packet as S_LeaveLobbyAnyUser;

        RoomManager.Instance.LeaveLobby(s_LeaveLobbyAnyUser.UserInfo);
        serverSession.ViewManager.ShowRemovedUser(0, s_LeaveLobbyAnyUser.UserInfo);

    }
}