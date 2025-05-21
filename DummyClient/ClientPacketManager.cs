using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ServerCore;

namespace DummyClient
{


    internal class ClientPacketManager
    {
        #region Singleton
        static ClientPacketManager _instance = new ClientPacketManager();
        public static ClientPacketManager Instance { get { return _instance; } }
        #endregion

        public Dictionary<ushort, Action<Session, byte[]>> handlers = new Dictionary<ushort, Action<Session, byte[]>> ();
        
        public ClientPacketManager()
        {
            handlers.Add((ushort)PacketId.S_CHAT, PacketHandler.S_ChatHandler);
        }

        public void InvokePacketHandler(Session session, byte[] buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer, 0);
            ushort packetId = BitConverter.ToUInt16(buffer, 2);

            bool result = handlers.TryGetValue(packetId, out var handler);
            if (!result)
            {
                return;
            }
            handler?.Invoke(session, buffer);
        }
    }
}
