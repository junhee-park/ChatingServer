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

namespace DummyClient
{
    public class ServerSession : PacketSession
    {
        public int UserId { get; private set; } = -1; // -1은 유저가 아직 로그인하지 않았음을 의미
        public string Nickname { get; set; } = string.Empty; // 유저 이름
        public string TempNickname { get; set; } = string.Empty; // 유저 이름 변경을 위한 임시 이름

        public ServerSession(Socket socket) : base(socket)
        {

        }

        public void Send(IMessage message)
        {
            string packetName = message.Descriptor.Name.Replace("_", string.Empty);
            MsgId packetId = (MsgId)System.Enum.Parse(typeof(MsgId), packetName);
            int packetSize = message.CalculateSize();
            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[packetSize + 4]);
            BitConverter.TryWriteBytes(segment.Array, (ushort)(packetSize + 4));
            BitConverter.TryWriteBytes(new ArraySegment<byte>(segment.Array, 2, segment.Count - 2), (ushort)packetId);
            Array.Copy(message.ToByteArray(), 0, segment.Array, 4, packetSize);

            RegisterSend(segment.Array);
        }

        public override void OnConnect(EndPoint endPoint)
        {

        }

        public override void OnDisconnect(EndPoint endPoint)
        {

        }

        public override void OnRecvPacket(ArraySegment<byte> data)
        {
            PacketManager.Instance.InvokePacketHandler(this, data);
        }

        public override void OnSend(int bytesTransferred)
        {

        }
    }
}
