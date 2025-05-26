using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ServerCore;

namespace DummyClient
{
    public static class PacketHandler
    {
        public static void S_ChatHandler(Session session, byte[] buffer)
        {
            ServerSession serverSession = session as ServerSession;
            S_Chat s_Chat = new S_Chat();
            s_Chat.Read(buffer);

            Console.WriteLine($"[{serverSession.testServerSessionName} -> User_{s_Chat.userId}]: {s_Chat.msg}");
        }
    }
}
