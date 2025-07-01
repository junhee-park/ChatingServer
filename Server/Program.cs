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
                            Console.WriteLine("세션 매니저 유저 목록");
                            foreach (var session in SessionManager.Instance.clientSessions.Values)
                            {
                                if (session.UserInfo != null)
                                {
                                    Console.WriteLine($"UserId: {session.UserInfo.UserId}, Nickname: {session.UserInfo.Nickname}, CurrentRoom: {session.Room?.roomInfo?.RoomId ?? -1}");
                                }
                            }
                            Console.WriteLine("룸 매니저 로비 유저 목록");
                            foreach (var userId in RoomManager.Instance.userIds)
                            {
                                if (SessionManager.Instance.clientSessions.TryGetValue(userId, out ClientSession clientSession))
                                {
                                    Console.WriteLine($"UserId: {clientSession.UserInfo.UserId}, Nickname: {clientSession.UserInfo.Nickname}, CurrentRoom: {clientSession.Room?.roomInfo?.RoomId ?? -1}");
                                }
                            }
                            Console.WriteLine("룸 매니저 방 목록");
                            foreach (var room in RoomManager.Instance.rooms.Values)
                            {
                                Console.WriteLine($"RoomId: {room.roomInfo.RoomId}, RoomName: {room.roomInfo.RoomName}, RoomMasterUserId: {room.roomInfo.RoomMasterUserId}");
                                foreach (var userInfo in room.roomInfo.UserInfos.Values)
                                {
                                    Console.WriteLine($"  UserId: {userInfo.UserId}, Nickname: {userInfo.Nickname}");
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
