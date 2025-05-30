using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server
{
    internal class ServerPacketManager
    {
        #region Singleton
        static ServerPacketManager _instance = new ServerPacketManager();
        public static ServerPacketManager Instance { get { return _instance; } }
        #endregion

        Dictionary<ushort, Action<Session, IMessage>> _handlers = new Dictionary<ushort, Action<Session, IMessage>> ();
        Dictionary<ushort, Func<ArraySegment<byte>, IMessage>> _makePacket = new Dictionary<ushort, Func<ArraySegment<byte>, IMessage>>();
        
        public ServerPacketManager()
        {
            _handlers.Add((ushort)MsgId.CChat, PacketHandler.C_ChatHandler);
            _makePacket.Add((ushort)MsgId.CChat, MakePacket<C_Chat>);
        }

        public T MakePacket<T>(ArraySegment<byte> buffer) where T : IMessage, new()
        {
            T packet = new T();
            packet.MergeFrom(buffer);

            return packet;
        }

        public void InvokePacketHandler(Session session, ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, 0);
            ushort packetId = BitConverter.ToUInt16(buffer.Array, 2);

            ArraySegment<byte> data = new ArraySegment<byte>(buffer.Array, 4, size - 4);
            bool result = _makePacket.TryGetValue(packetId, out var makePacketFunc);
            if (!result)
            {
                return;
            }
            IMessage packet = makePacketFunc.Invoke(data);

            result = _handlers.TryGetValue(packetId, out var handler);
            if (!result)
            {
                return;
            }
            handler?.Invoke(session, packet);
        }
    }
}
