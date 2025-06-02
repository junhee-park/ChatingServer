using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DummyClient;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

public static class PacketHandler
{
    public static void S_ChatHandler(Session session, IMessage packet)
    {
        ServerSession serverSession = session as ServerSession;
        S_Chat s_ChatPacket = packet as S_Chat;

        if (s_ChatPacket.UserId == 0)
            Console.WriteLine($"[{serverSession.testServerSessionName} -> User_{s_ChatPacket.UserId}]: {s_ChatPacket.Msg}");
    }
}