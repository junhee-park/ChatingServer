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

        public Room(int id, string name, int roomMasterUserId)
        {
            roomInfo = new RoomInfo();
            roomInfo.RoomId = id;
            roomInfo.RoomName = name;
            roomInfo.RoomMasterUserId = roomMasterUserId;
        }

        public void AddUser(ClientSession session)
        {
            roomInfo.UserIds.Add(session.UserId);
        }

        public bool LeaveUser(int userId)
        {
            return roomInfo.UserIds.Remove(userId);
        }

        /// <summary>
        /// 해당 룸에 있는 모든 유저에게 메시지를 브로드캐스트합니다.
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(IMessage message)
        {
            foreach (var userId in roomInfo.UserIds)
            {
                SessionManager.Instance.sessions.TryGetValue(userId, out Session session);
                var clinetSession = session as ClientSession;
                clinetSession?.Send(message);
            }
        }
    }
}
