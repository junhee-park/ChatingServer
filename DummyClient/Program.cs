using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using ServerCore;

namespace DummyClient
{
    internal class Program
    {
        static Session serverSession;
        static List<Session> sessions = new List<Session>();

        static object _lock = new object();

        // testConnection * (1000 / testSendMs) = tps
        static int testConnection = 1;
        static int testSendMs = 100;


        static void Main(string[] args)
        {
            Thread.Sleep(2000);

            Console.WriteLine("Hello, Client!");

            IPAddress[] iPAddress = Dns.GetHostAddresses(Dns.GetHostName());
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress[1], 7777);

            Connector connector = new Connector();
            connector.Connect(iPEndPoint,
                (saea) => {
                    lock (_lock)
                    {
                        serverSession = new TestServerSession(saea.ConnectSocket);
                        //serverSession = new ServerSession(saea.ConnectSocket);
                        serverSession.OnConnect(saea.RemoteEndPoint);
                        sessions.Add(serverSession);
                        return serverSession;
                    }
                },
                testConnection);

            while (true)
            {
                Thread.Sleep(1000);

                if (sessions.Count == testConnection)
                    break;
            }

            SetNickname();

            while (true)
            {
                Console.WriteLine("\nPress Q to set nickname, W to create room, E to list rooms, Spacebar to send test messages, or Escape to exit.");
                var readKey = Console.ReadKey();
                switch (readKey)
                {
                    case { Key: ConsoleKey.Q }:
                        {
                            SetNickname();
                            break;
                        }
                    case { Key: ConsoleKey.W }:
                        {
                            Console.WriteLine("\nW Key Pressed. Sending C_CreateRoom Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_CreateRoom c_CreateRoom = new C_CreateRoom();
                            Console.Write("Enter Room Name:");
                            c_CreateRoom.RoomName = Console.ReadLine();

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {
                                Console.WriteLine($"C_CreateRoom Send Complete!");
                            };

                            testServerSession.Send(c_CreateRoom);
                            break;
                        }
                    case { Key: ConsoleKey.E }:
                        {
                            Console.WriteLine("\nE Key Pressed. Sending C_RoomList Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_RoomList c_RoomList = new C_RoomList();

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {
                                Console.WriteLine($"C_RoomList Send Complete!");
                                foreach( var room in RoomManager.Instance.rooms )
                                {
                                    Console.WriteLine($"[{room.Key}] {room.Value.RoomName}");
                                }
                            };

                            testServerSession.Send(c_RoomList);
                            break;
                        }
                    case { Key: ConsoleKey.R }:
                        {
                            Console.WriteLine("\nE Key Pressed. Sending C_DeleteRoom Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_DeleteRoom c_DeleteRoom = new C_DeleteRoom();

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {
                                foreach(var room in RoomManager.Instance.rooms)
                                {
                                    Console.WriteLine($"[{room.Key}] {room.Value.RoomName}");
                                }
                            };

                            testServerSession.Send(c_DeleteRoom);
                            break;
                        }
                    case { Key: ConsoleKey.Escape }:
                        {
                            Console.WriteLine("\nEscape Key Pressed. Exiting...");
                            return;
                        }
                    case { Key: ConsoleKey.Spacebar }:
                        {
                            Console.WriteLine("\nSpacebar Key Pressed. Sending Test Messages...");
                            TestBoradcast();
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("\nPress Spacebar to send test messages or Escape to exit.");
                            break;
                        }
                }
            }

            //TestRtt();
            Console.ReadKey();
        }

        public static void SetNickname()
        {
            Console.WriteLine("\nQ Key Pressed. Sending C_SetNickname Messages...");

            var testServerSession = serverSession as TestServerSession;
            C_SetNickname c_SetNickname = new C_SetNickname();
            Console.Write("Enter your nickname:");
            string nickname = Console.ReadLine();
            testServerSession.TempNickname = nickname;
            c_SetNickname.Nickname = nickname;

            // 테스트 로그
            string temp = testServerSession.Nickname;
            testServerSession.testLog = () =>
            {
                var temp1 = temp;
                Console.WriteLine($"{temp} -> {testServerSession.Nickname}");

            };

            testServerSession.Send(c_SetNickname);
        }

        public static void TestRtt()
        {
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                if (stopwatch.IsRunning == false)
                    stopwatch.Start();

                if (stopwatch.ElapsedMilliseconds >= 30000)
                {
                    stopwatch.Stop();
                    var stream = File.CreateText("./test.txt");
                    foreach (var item in sessions)
                    {
                        var session = item as TestServerSession;
                        long totalRtt = 0;
                        for (int i = 0; i < session.rtts.Count; i++)
                        {
                            totalRtt += session.rtts[i];
                            if (session.testServerSessionName == "TestSession_1")
                            {
                                stream.WriteLine($"{session.testServerSessionName} - RTT {i + 1}: {(double)session.rtts[i] / 10000} ms");
                                //stream.WriteLine($"{session.testServerSessionName} - RTT {i + 1}: {(double)session.rtts[i]} ms");
                            }
                        }
                        var tmp = (double)totalRtt / session.rtts.Count;
                        stream.WriteLine($"{session.testServerSessionName} - Total Packet Count: {session.rtts.Count}, Min RTT: {(double)session.minRttMs / 10000} ms, Max RTT: {(double)session.maxRttMs / 10000} ms, Avg RTT: {tmp / 10000} ms");
                        //stream.WriteLine($"{session.testServerSessionName} - Total Packet Count: {session.rtts.Count}, Min RTT: {session.minRttMs} ms, Max RTT: {session.maxRttMs} ms, Avg RTT: {tmp} ms");
                    }
                    stream.Dispose();
                    break;
                }

                TestBoradcast();
                Thread.Sleep(testSendMs);
            }
        }

        public static void TestBoradcast()
        {
            C_TestChat c_Test_Chat = new C_TestChat();
            c_Test_Chat.Chat = new C_Chat(); ;
            c_Test_Chat.Chat.Msg = $"Test Message~~~~~~~";

            for (int i = 0; i < sessions.Count; i++)
            {
                TestServerSession session = sessions[i] as TestServerSession;

                c_Test_Chat.TickCount = DateTime.UtcNow.Ticks;
                //c_Chat.TickCount = Environment.TickCount64;

                session.Send(c_Test_Chat);
            }
        }
    }

    public class TestServerSession : ServerSession
    {
        public static int count = 0;
        public string testServerSessionName;
        public long minRttMs = long.MaxValue;
        public long maxRttMs = 0;
        public List<long> rtts = new List<long>();

        public Action testLog;
        public TestServerSession(Socket socket) : base(socket)
        {
            testServerSessionName = $"TestSession_{Interlocked.Increment(ref count)}";
        }

        public void TestCompareRtt(S_TestChat s_ChatPacket)
        {
            long rttMs = DateTime.UtcNow.Ticks - s_ChatPacket.TickCount;
            //long rttMs = Environment.TickCount64 - s_ChatPacket.TickCount;
            if (rttMs < minRttMs)
                minRttMs = rttMs;
            if (rttMs > maxRttMs)
                maxRttMs = rttMs;
            rtts.Add(rttMs);

            if (s_ChatPacket.Chat.UserId == 0)
                Console.WriteLine($"[{testServerSessionName} -> User_{s_ChatPacket.Chat.UserId}]: {s_ChatPacket.Chat.Msg}");

        }

        public override void OnConnect(EndPoint endPoint)
        {
            Console.WriteLine($"[{testServerSessionName}] Connected to {endPoint}");
        }

        public override void OnDisconnect(EndPoint endPoint)
        {
            Console.WriteLine($"[{testServerSessionName}] Disconnected from {endPoint}");
        }

        public override void OnRecvPacket(ArraySegment<byte> data)
        {
            PacketManager.Instance.InvokePacketHandler(this, data);
            testLog?.Invoke();
        }

        public override void OnSend(int bytesTransferred)
        {
            //Console.WriteLine($"[{testServerSessionName}] Sent {bytesTransferred} bytes.");
        }
    }

}
