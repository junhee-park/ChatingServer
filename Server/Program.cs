using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, Server!");

            IPAddress[] iPAddress = Dns.GetHostAddresses(Dns.GetHostName());
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress[1], 7777);

            Listener listener = new Listener();
            listener.Init(iPEndPoint, SessionManager.Instance.CreateSession);

            while (true)
            {
                // 서버가 실행 중일 때 콘솔에서 입력을 받기 위한 부분
                Console.WriteLine("Press 'Q' to display all users' current locations or any other key to continue...");
                var key = Console.ReadKey();
                switch (key)
                {
                    case { Key: ConsoleKey.Q }:
                        {
                            Console.WriteLine("현재 모든 유저가 어디에 있는지 표시");
                            foreach (var session in SessionManager.Instance.clientSessions.Values)
                            {
                                if (session.UserInfo != null)
                                {
                                    Console.WriteLine($"UserId: {session.UserInfo.UserId}, Nickname: {session.UserInfo.Nickname}, CurrentRoom: {session.Room?.roomInfo?.RoomId ?? -1}");
                                }
                            }
                            break;
                        }
                }
                Thread.Sleep(0);
            }
        }
    }
}
