using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using ServerCore;
using Google.Protobuf.Protocol;
using Server;

/// <summary>
/// 전송된 패킷을 처리하는 클래스.
/// 패킷 이름 + Handler 규칙으로 핸들러 함수를 작성해야 해당 패킷 받았을 때 핸들러가 실행됨.
/// </summary>
public static class PacketHandler
{
    public static void C_ChatHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Chat c_ChatPacket = packet as C_Chat;

        //C_Chat c_Chat = new C_Chat();
        //c_Chat.Read(buffer);

        // 유저 아이디 추출
        int userId = clientSession.UserId;

        // 패킷 생성
        S_Chat s_Chat = new S_Chat();
        s_Chat.UserId = userId;
        s_Chat.Msg = c_ChatPacket.Msg;
        s_Chat.TickCount = c_ChatPacket.TickCount;

        Program.Boardcast(s_Chat);
    }

    public static void C_PingHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Ping c_PingPacket = packet as C_Ping;
        // Ping 응답 패킷 생성
        S_Ping s_Ping = new S_Ping();
        // Ping 응답 전송
        clientSession.Send(s_Ping);
    }

    public static void C_TestChatHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_TestChat c_TestChatPacket = packet as C_TestChat;
        // 유저 아이디 추출
        int userId = clientSession.UserId;
        // 패킷 생성
        S_TestChat s_TestChat = new S_TestChat();
        S_Chat s_chat = new S_Chat();
        s_chat.UserId = userId;
        s_chat.Msg = c_TestChatPacket.Chat.Msg;
        s_TestChat.Chat = s_chat;
        s_TestChat.TickCount = c_TestChatPacket.TickCount;
        Program.Boardcast(s_TestChat);
    }
}