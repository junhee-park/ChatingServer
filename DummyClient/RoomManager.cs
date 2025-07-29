using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;

public class RoomManager
{
    public RoomInfo CurrentRoom { get; set; } = null; // 현재 참여 중인 방 정보

    public MapField<int, RoomInfo> Rooms { get; private set; } = new MapField<int, RoomInfo>();
    public MapField<int, UserInfo> UserInfos { get; private set; } = new MapField<int, UserInfo>(); // 로비에 존재하는 유저의 id목록

    object _lock = new object();

    /// <summary>
    /// 방에 유저를 추가합니다.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="userInfo"></param>
    public void AddUserToRoom(int roomId, UserInfo userInfo)
    {
        lock (_lock)
        {
            if (Rooms.TryGetValue(roomId, out RoomInfo room))
            {
                room.UserInfos.TryAdd(userInfo.UserId, userInfo);
                UserInfos.Remove(userInfo.UserId); // 로비에서 제거
            }
        }
    }

    public void AddUserToLobby(UserInfo userInfo)
    {
        lock (_lock)
        {
            UserInfos[userInfo.UserId] = userInfo;
        }
    }

    public void CreateRoom(RoomInfo roomInfo)
    {
        lock (_lock)
        {
            Rooms.TryAdd(roomInfo.RoomId, roomInfo);
        }
    }

    public void LeaveRoom(int roomId, UserInfo userInfo)
    {
        lock (_lock)
        {
            Rooms.TryGetValue(roomId, out RoomInfo roomInfo);
            if (roomInfo != null)
                roomInfo.UserInfos.Remove(userInfo.UserId);
            UserInfos.TryAdd(userInfo.UserId, userInfo); // 유저를 로비로 이동
        }
    }

    public RoomInfo GetRoomInfo(int roomId)
    {
        lock (_lock)
        {
            if (Rooms.TryGetValue(roomId, out RoomInfo roomInfo))
                return roomInfo;

            return null;
        }
    }

    public RoomInfo GetRandomRoomInfo()
    {
        lock (_lock)
        {
            if (Rooms.Count == 0)
                return null;
            // 랜덤으로 방을 선택
            Random random = new Random();
            int randomIndex = random.Next(Rooms.Count);
            return Rooms.ElementAt(randomIndex).Value;
        }
    }


    public void LeaveLobby(UserInfo userInfo)
    {
        lock (_lock)
        {
            UserInfos.Remove(userInfo.UserId);
        }
    }

    public void DeleteRoom(int roomId)
    {
        lock (_lock)
        {
            Rooms.Remove(roomId);
        }
    }

    public void Refresh(MapField<int, RoomInfo> roomInfoList)
    {
        Rooms.Clear();
        foreach (var room in roomInfoList)
        {
            Rooms.Add(room.Key, room.Value);
        }
    }

    public void RefreshUserInfos(MapField<int, UserInfo> userInfos)
    {
        UserInfos.Clear();
        foreach (var user in userInfos)
        {
            UserInfos.Add(user.Key, user.Value);
        }
    }
    
    public string ChangeNickname(string nickname, int userId)
    {
        lock (_lock)
        {
            string oldNickname = UserInfos[userId].Nickname;
            UserInfos[userId].Nickname = nickname;
            return oldNickname;
        }
    }
}