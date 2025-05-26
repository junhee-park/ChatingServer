using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ServerCore;

namespace Server
{
    public static class PacketHandler
    {
        public static void C_ChatHandler(Session session, byte[] buffer)
        {
            ClientSession clientSession = session as ClientSession;

            C_Chat c_Chat = new C_Chat();
            c_Chat.Read(buffer);

            // 유저 아이디 추출
            int userId = clientSession.UserId;

            // 패킷 생성
            S_Chat s_Chat = new S_Chat();
            s_Chat.userId = userId;
            s_Chat.msg = c_Chat.msg;
            s_Chat.Write(out byte[] data);

            Program.Boardcast(data);
        }
    }
}
