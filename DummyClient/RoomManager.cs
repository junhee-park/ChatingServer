using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;

public class RoomManager
{
    #region Singleton
    static RoomManager _instance = new RoomManager();
    public static RoomManager Instance { get { return _instance; } }
    #endregion

    public string TempRoomName { get; set; } = string.Empty; // 방 이름 생성을 위한 임시 이름
    public RoomInfo CurrentRoom { get; set; } = null; // 현재 참여 중인 방 정보

    public Dictionary<int, RoomInfo> Rooms { get; private set; } = new Dictionary<int, RoomInfo>();
    public Dictionary<int, UserInfo> UserInfos { get; private set; } = new Dictionary<int, UserInfo>(); // 로비에 존재하는 유저의 id목록

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
                room.UserInfos.Add(userInfo);
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

    public void CreateRoom(int roomId, int roomMasterId)
    {
        lock (_lock)
        {
            RoomInfo roomInfo = new RoomInfo
            {
                RoomId = roomId,
                RoomName = TempRoomName,
                RoomMasterUserId = roomMasterId
            };
            TempRoomName = string.Empty; // 방 이름 초기화
            roomInfo.UserInfos.Add(UserInfos[roomMasterId]); // 방장 유저 추가
            Rooms.Add(roomInfo.RoomId, roomInfo);
            CurrentRoom = roomInfo; // 현재 방 정보 설정
            UserInfos.Remove(roomMasterId); // 방장 유저는 로비에서 제거
        }
    }

    public void LeaveRoom(UserInfo userInfo)
    {
        lock (_lock)
        {
            Rooms.TryGetValue(CurrentRoom.RoomId, out RoomInfo roomInfo);
            roomInfo.UserInfos.Remove(userInfo);
            UserInfos.Add(userInfo.UserId, userInfo); // 유저를 로비로 이동
        }
    }

    public void Refresh(Google.Protobuf.Collections.RepeatedField<RoomInfo> roomInfoList)
    {
        Rooms.Clear();
        foreach (var room in roomInfoList)
        {
            Rooms.Add(room.RoomId, room);
        }
    }

    public void RefreshUserInfos(RepeatedField<UserInfo> userInfos)
    {
        UserInfos.Clear();
        foreach (var user in userInfos)
        {
            UserInfos.Add(user.UserId, user);
        }
    }
}