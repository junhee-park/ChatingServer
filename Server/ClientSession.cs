using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using ServerCore;
using Google.Protobuf.Protocol;

namespace Server
{
    public class ClientSession : PacketSession
    {
        public UserInfo UserInfo { get; set; } = new UserInfo(); // 유저 정보
        public Room Room { get; set; } = null; // 현재 방 정보 (null이면 방에 없음)

        public UserState CurrentState { get; set; } = UserState.Lobby; // 현재 상태 (Lobby, Room 등)

        public DateTime LastRecvDate { get; set; } = DateTime.UtcNow;
        public bool IsPing { get; set; } // 핑 요청 여부

        public ClientSession(Socket socket, int userId) : base(socket)
        {
            UserInfo.UserId = userId;
            UserInfo.Nickname = $"User_{userId}"; // 기본 유저 닉네임 설정
        }

        public void Send(IMessage message)
        {
            string packetName = message.Descriptor.Name.Replace("_", string.Empty);
            MsgId packetId = (MsgId)Enum.Parse(typeof(MsgId), packetName);
            int packetSize = message.CalculateSize();
            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[packetSize + 4]);
            BitConverter.TryWriteBytes(segment.Array, (ushort)(packetSize + 4));
            BitConverter.TryWriteBytes(new ArraySegment<byte>(segment.Array, 2, segment.Count - 2), (ushort)packetId);
            Array.Copy(message.ToByteArray(), 0, segment.Array, 4, packetSize);

            RegisterSend(segment.Array);
        }

        public override void OnRecvPacket(ArraySegment<byte> data)
        {
            ushort packetId = BitConverter.ToUInt16(data.Array, 2);
            Console.WriteLine($"{DateTime.UtcNow} {(MsgId)packetId} {UserInfo.UserId}");
            PacketManager.Instance.InvokePacketHandler(this, data);
            LastRecvDate = DateTime.UtcNow;
            IsPing = false;
        }

        public override void OnSend(int bytesTransferred)
        {

        }

        public override void OnConnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnect User_{UserInfo.UserId} {endPoint.ToString()}");
        }

        public override void OnDisconnect(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnect User_{UserInfo.UserId} {endPoint.ToString()}");
            
            RoomManager.Instance.Enqueue(RoomManager.Instance.DisconnectUser, this);
        }
    }
}
