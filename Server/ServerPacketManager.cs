using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ServerCore;

namespace Server
{
    internal class ServerPacketManager
    {
        #region Singleton
        static ServerPacketManager _instance = new ServerPacketManager();
        public static ServerPacketManager Instance { get { return _instance; } }
        #endregion

        public Dictionary<ushort, Action<Session, ArraySegment<byte>>> handlers = new Dictionary<ushort, Action<Session, ArraySegment<byte>>> ();
        
        public ServerPacketManager()
        {
            handlers.Add((ushort)PacketId.C_CHAT, PacketHandler.C_ChatHandler);
        }

        public void InvokePacketHandler(Session session, ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, 0);
            ushort packetId = BitConverter.ToUInt16(buffer.Array, 2);

            bool result = handlers.TryGetValue(packetId, out var handler);
            if (!result)
            {
                return;
            }
            handler?.Invoke(session, buffer);
        }
    }
}
