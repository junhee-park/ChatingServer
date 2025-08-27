using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DummyClient;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using static System.Net.Mime.MediaTypeNames;


public class ConsoleViewManager : IViewManager
{
    bool isLobby = true;
    public void ShowRoomList(MapField<int, RoomInfo> roomInfos)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //foreach (var room in roomInfos)
        //{
        //    Console.WriteLine($"Room ID: {room.Key}, Name: {room.Value.RoomName}, Master: {room.Value.RoomMasterUserId}");
        //    foreach (var user in room.Value.UserInfos.Values)
        //    {
        //        Console.WriteLine($" - User ID: {user.UserId}, Nickname: {user.Nickname}");
        //    }
        //}
    }

    public void ShowText(string text)
    {
        //Console.WriteLine(text);
    }

    public void ShowText(S_ChatBc s_Chat)
    {
        throw new NotImplementedException();
    }

    public void ShowRoomUserList(MapField<int, UserInfo> userInfos)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //foreach (var user in userInfos.Values)
        //{
        //    Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");

        //}
    }

    public void ShowLobbyUserList(MapField<int, UserInfo> userInfos)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //foreach (var user in userInfos.Values)
        //{
        //    Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");

        //}
    }

    public void ShowLobbyUserList(Dictionary<int, UserInfo> userInfos)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //foreach (var user in userInfos.Values)
        //{
        //    Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");
        //}
    }

    public void ShowChangedNickname(UserInfo userInfo, string newName)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        userInfo.Nickname = newName;
        //Console.WriteLine($"User {userInfo.UserId} changed nickname to {newName}");
    }

    private void ShowLobbyScreen()
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
    }

    private void ShowRoomScreen()
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
    }

    public void ShowChangedScreen(UserState userState)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        if (userState == UserState.Lobby)
        {
            ShowLobbyScreen();
        }
        else if (userState == UserState.Room)
        {
            ShowRoomScreen();
        }
        else
        {
            throw new Exception("Unknown user state.");
        }
    }

    public void ShowAddedRoom(RoomInfo roomInfo)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //// 추가된 방 정보를 콘솔에 출력
        //Console.WriteLine($"Added Room ID: {roomInfo.RoomId}, Name: {roomInfo.RoomName}, Master: {roomInfo.RoomMasterUserId}");

    }

    public void ShowAddedUser(int roomId, UserInfo userInfo)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //// 특정 방에 유저가 추가되었다고 콘솔창에 표시
        //Console.WriteLine($"Added Room ID: {roomId}, Nickname: {userInfo.Nickname}, UserId: {userInfo.UserId}");

    }

    public void ShowRemovedUser(int roomId, UserInfo userInfo)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //// 특정 방에 유저가 떠났다고 콘솔창에 표시
        //Console.WriteLine($"Removed Room ID: {roomId}, Nickname: {userInfo.Nickname}, UserId: {userInfo.UserId}");
    }

    public void ShowRemovedRoom(int roomId)
    {
        //Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        //// 방이 삭제되었다고 콘솔창에 표시
        //Console.WriteLine($"Removed Room ID: {roomId}");
    }
}