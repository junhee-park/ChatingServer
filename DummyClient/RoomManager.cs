using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;

namespace DummyClient
{
    public class RoomManager
    {
        #region Singleton
        static RoomManager _instance = new RoomManager();
        public static RoomManager Instance { get { return _instance; } }
        #endregion

        public string TempRoomName { get; set; } = string.Empty; // 방 이름 생성을 위한 임시 이름
        public RoomInfo CurrentRoom { get; set; } = null; // 현재 참여 중인 방 정보

        public Dictionary<int, RoomInfo> rooms = new Dictionary<int, RoomInfo>();

        public void RemoveRoom(int roomId)
        {
            if (rooms.ContainsKey(roomId))
            {
                rooms.Remove(roomId);
            }
        }

        public void Refresh(Google.Protobuf.Collections.RepeatedField<RoomInfo> roomInfoList)
        {
            rooms.Clear();
            foreach (var room in roomInfoList)
            {
                rooms.Add(room.RoomId, room);
            }
        }
    }
}
