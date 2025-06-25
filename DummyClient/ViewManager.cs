using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using static System.Net.Mime.MediaTypeNames;

public interface IViewManager
{
    void ShowText(string text);
    void ShowText(S_Chat s_Chat);
    void ShowRoomList(RepeatedField<RoomInfo> roomInfos);
    void ShowRoomUserList(RepeatedField<UserInfo> userInfos);
    void ShowLobbyUserList(RepeatedField<UserInfo> userInfos);
    void ShowLobbyUserList(Dictionary<int, UserInfo> userInfos);
    void ShowChangedNickname(UserInfo userInfo, string newName);
    void ShowLobbyScreen();
    void ShowRoomScreen();
    void ShowAddedRoom(RoomInfo roomInfo);
    void ShowAddedUser(int roomId, UserInfo userInfo);
    void ShowRemovedUser(int roomId, UserInfo userInfo);
}

public class ConsoleViewManager : IViewManager
{
    public void ShowRoomList(RepeatedField<RoomInfo> roomInfos)
    {
        foreach (var room in roomInfos)
        {
            Console.WriteLine($"Room ID: {room.RoomId}, Name: {room.RoomName}, Master: {room.RoomMasterUserId}");
            foreach (var user in room.UserInfos)
            {
                Console.WriteLine($" - User ID: {user.UserId}, Nickname: {user.Nickname}");
            }
        }
    }

    public void ShowText(string text)
    {
        Console.WriteLine(text);
    }

    public void ShowText(S_Chat s_Chat)
    {
        throw new NotImplementedException();
    }

    public void ShowRoomUserList(RepeatedField<UserInfo> userInfos)
    {
        Console.WriteLine("ShowRoomUserList");
        foreach (var user in userInfos)
        {
            Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");

        }
    }

    public void ShowLobbyUserList(RepeatedField<UserInfo> userInfos)
    {
        Console.WriteLine("ShowLobbyUserList");
        foreach (var user in userInfos)
        {
            Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");

        }
    }

    public void ShowLobbyUserList(Dictionary<int, UserInfo> userInfos)
    {
        Console.WriteLine("ShowLobbyUserList");
        foreach (var user in userInfos.Values)
        {
            Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");
        }
    }

    public void ShowChangedNickname(UserInfo userInfo, string newName)
    {
        RoomManager.Instance.UserInfos[userInfo.UserId].Nickname = newName;
        Console.WriteLine($"User {userInfo.UserId} changed nickname to {newName}");
    }

    public void ShowLobbyScreen()
    {

    }

    public void ShowRoomScreen()
    {
        
    }

    public void ShowAddedRoom(RoomInfo roomInfo)
    {
        // 추가된 방 정보를 콘솔에 출력
        Console.WriteLine($"Added Room ID: {roomInfo.RoomId}, Name: {roomInfo.RoomName}, Master: {roomInfo.RoomMasterUserId}");

    }

    public void ShowAddedUser(int roomId, UserInfo userInfo)
    {
        // 특정 방에 유저가 추가되었다고 콘솔창에 표시
        Console.WriteLine($"Added Room ID: {roomId}, Nickname: {userInfo.Nickname}, UserId: {userInfo.UserId}");

    }

    public void ShowRemovedUser(int roomId, UserInfo userInfo)
    {
        // 특정 방에 유저가 떠났다고 콘솔창에 표시
        Console.WriteLine($"Removed Room ID: {roomId}, Nickname: {userInfo.Nickname}, UserId: {userInfo.UserId}");
    }
}