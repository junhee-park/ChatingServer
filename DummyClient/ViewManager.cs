using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;

public interface IViewManager
{
    void ShowText(string text);
    void ShowText(S_Chat s_Chat);
    void ShowRoomList(RepeatedField<RoomInfo> roomInfos);
    void ShowRoomUserList(RepeatedField<UserInfo> userInfos);
    void ShowLobbyUserList(RepeatedField<UserInfo> userInfos);
    void ShowLobbyUserList(Dictionary<int, UserInfo> userInfos);
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
        foreach (var user in userInfos)
        {
            Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");

        }
    }

    public void ShowLobbyUserList(RepeatedField<UserInfo> userInfos)
    {
        foreach (var user in userInfos)
        {
            Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");

        }
    }

    public void ShowLobbyUserList(Dictionary<int, UserInfo> userInfos)
    {
        foreach (var user in userInfos.Values)
        {
            Console.WriteLine($"User ID: {user.UserId}, Nickname: {user.Nickname}");
        }
    }
}