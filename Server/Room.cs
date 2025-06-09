using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;

namespace Server
{
    public class Room
    {
        public RoomInfo roomInfo;
        public Dictionary<int, ClientSession> users = new Dictionary<int, ClientSession>();

        public Room(int id, string name, int roomMasterUserId)
        {
            roomInfo = new RoomInfo();
            roomInfo.RoomId = id;
            roomInfo.RoomName = name;
            roomInfo.RoomMasterUserId = roomMasterUserId;
        }

        public bool AddUser(ClientSession session)
        {
            bool result = users.TryAdd(session.UserId, session);
            if (result)
                roomInfo.UserIds.Add(session.UserId);
            return result;
        }

        public bool LeaveUser(int userId)
        {
            bool result = users.Remove(userId);
            if (result)
                roomInfo.UserIds.Remove(userId);

            return result;
        }

        /// <summary>
        /// 해당 룸에 있는 모든 유저에게 메시지를 브로드캐스트합니다.
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(IMessage message)
        {
            foreach (var user in users.Values)
            {
                user.Send(message);
            }
        }
    }
}
