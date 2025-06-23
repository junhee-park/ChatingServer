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
    public enum State
    {
        None = 0,
        Lobby = 1,
        Room = 2
    }

    public class ClientSession : PacketSession
    {
        public UserInfo UserInfo { get; set; } = new UserInfo(); // 유저 정보
        public Room Room { get; set; } = null; // 현재 방 정보 (null이면 방에 없음)

        public State CurrentState { get; set; } = State.None; // 현재 상태 (Lobby, Room 등)

        public ClientSession(Socket socket, int userId) : base(socket)
        {
            UserInfo.UserId = userId;
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
            PacketManager.Instance.InvokePacketHandler(this, data);
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
        }
    }
}
