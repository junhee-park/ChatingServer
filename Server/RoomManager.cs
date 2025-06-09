using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class RoomManager
    {
        #region Singleton
        static RoomManager _instance = new RoomManager();
        public static RoomManager Instance { get { return _instance; } }
        #endregion

        public int nextRoomId = 1; // Room ID를 생성하기 위한 카운터
        public ConcurrentDictionary<int, Room> rooms = new ConcurrentDictionary<int, Room>();
        public ConcurrentDictionary<int, ClientSession> users = new ConcurrentDictionary<int, ClientSession>();

        public Room CreateRoom(string roomName, int roomMasterId)
        {
            Room room = new Room(Interlocked.Increment(ref nextRoomId), roomName, roomMasterId);
            rooms.TryAdd(room.roomInfo.RoomId, room);
            return room;
        }

        public Room GetRoom(int roomId)
        {
            if (rooms.TryGetValue(roomId, out Room room))
                return room;
            return null;
        }

        public bool RemoveRoom(int roomId, int userId)
        {
            if (rooms.TryGetValue(roomId, out Room room))
                return false;
            if (room.roomInfo.RoomMasterUserId != userId)
                return false; // 방장만 방을 삭제할 수 있음

            foreach (var user in room.users)
            {
                users.TryAdd(user.Key, user.Value);
            }
            room.users.Clear();
            
            return rooms.TryRemove(new KeyValuePair<int, Room>(roomId, room));
        }

        public bool AddUserToRoom(int roomId, ClientSession session)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                users[session.UserId] = session; // 세션을 사용자 목록에 추가
                return room.AddUser(session);
            }
            return false;
        }

        public bool LeaveUserFromRoom(int roomId, int userId)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                if (!users.Remove(userId, out ClientSession value))
                {
                    return false; // 사용자 세션이 존재하지 않음
                }
                return room.LeaveUser(userId);
            }
            return false;
        }


    }
}
