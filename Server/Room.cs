using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

namespace Server
{
    public class Room
    {
        public RoomInfo roomInfo;
        public object _lock = new object(); // 멀티스레드 환경에서 안전하게 사용하기 위한 락

        public Room(int id, string name, int roomMasterUserId)
        {
            roomInfo = new RoomInfo();
            roomInfo.RoomId = id;
            roomInfo.RoomName = name;
            roomInfo.RoomMasterUserId = roomMasterUserId;
        }

        public void AddUser(ClientSession session)
        {
            lock (_lock)
            {
                if (roomInfo.UserInfos.ContainsKey(session.UserInfo.UserId))
                {
                    Console.WriteLine($"User {session.UserInfo.UserId} is already in the room {roomInfo.RoomId}.");
                    return; // 이미 방에 있는 유저는 추가하지 않음
                }
                roomInfo.UserInfos.Add(session.UserInfo.UserId, session.UserInfo);
            }
        }

        public bool LeaveUser(int userId)
        {
            lock (_lock)
            {
                bool result = false;
                foreach (var userInfo in roomInfo.UserInfos.Values)
                {
                    if (userInfo.UserId == userId)
                    {
                        result = roomInfo.UserInfos.Remove(userInfo.UserId);
                        break;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// 해당 룸에 있는 모든 유저에게 메시지를 브로드캐스트합니다.
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(IMessage message)
        {
            lock (_lock)
            {
                if (roomInfo.UserInfos.Count == 0)
                {
                    Console.WriteLine($"No users in room {roomInfo.RoomId} to broadcast message.");
                    return; // 방에 유저가 없으면 브로드캐스트하지 않음
                }
                foreach (var userInfo in roomInfo.UserInfos.Values)
                {
                    SessionManager.Instance.clientSessions.TryGetValue(userInfo.UserId, out ClientSession clinetSession);
                    clinetSession?.Send(message);
                }
            }
        }
    }
}
