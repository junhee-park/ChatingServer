using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

namespace Server
{
    public class RoomManager
    {
        #region Singleton
        static RoomManager _instance = new RoomManager();
        public static RoomManager Instance { get { return _instance; } }
        #endregion

        public int nextRoomId = 0; // Room ID를 생성하기 위한 카운터
        public ConcurrentDictionary<int, Room> rooms = new ConcurrentDictionary<int, Room>();
        public HashSet<int> userIds = new HashSet<int>(); // 로비에 존재하는 유저의 id목록

        object _lock = new object();

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

        public bool RemoveRoom(int roomId, int masterUserId)
        {
            // 방 삭제
            if (!rooms.TryGetValue(roomId, out Room room))
                return false;
            if (room.roomInfo.RoomMasterUserId != masterUserId)
                return false; // 방장만 방을 삭제할 수 있음
            return rooms.TryRemove(new KeyValuePair<int, Room>(roomId, room));
        }

        public void AddUserToRoom(int roomId, ClientSession session)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                room.AddUser(session);
            }
        }

        public void AddUserToLobby(int userId)
        {
            lock (_lock)
            {
                userIds.Add(userId); // 로비에 유저 추가
            }
        }

        public bool LeaveUserFromRoom(int roomId, int userId)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                return room.LeaveUser(userId);
            }
            return false;
        }

        public bool LeaveUserFromLobby(int userId)
        {
            lock (_lock)
            {
                return userIds.Remove(userId); // 로비에서 유저 제거
            }
        }

        public void BroadcastToLobby(IMessage message)
        {
            lock ( _lock)
            {
                foreach (var userId in userIds)
                {
                    if (SessionManager.Instance.clientSessions.TryGetValue(userId, out ClientSession clientSession))
                    {
                        clientSession.Send(message);
                    }
                }
            }
        }
    }
}
