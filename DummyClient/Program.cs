using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using ServerCore;
using static System.Net.Mime.MediaTypeNames;

namespace DummyClient
{
    internal class Program
    {
        public static ServerSession serverSession;
        static List<Session> sessions = new List<Session>();

        static object _lock = new object();

        // testConnection * (1000 / testSendMs) = tps
        static int testConnection = 10;
        static int testSendMs = 1000;


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
                        serverSession.InitViewManager(new ConsoleViewManager());
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

            //CommonTestLoop();

            DummyTest();
            Console.ReadKey();
        }

        public static void SetNickname()
        {
            Console.WriteLine("\nQ Key Pressed. Sending C_SetNickname Messages...");

            var testServerSession = serverSession as TestServerSession;
            C_SetNickname c_SetNickname = new C_SetNickname();
            Console.Write("Enter your nickname:");
            string nickname = Console.ReadLine();
            c_SetNickname.Nickname = nickname;

            // 테스트 로그
            string temp = testServerSession.UserInfo.Nickname;
            testServerSession.testLog = () =>
            {


            };

            testServerSession.Send(c_SetNickname);
        }

        public static void CommonTestLoop()
        {
            while (true)
            {
                Console.WriteLine("\nPress Q to set nickname, W to create room, E to list rooms, Spacebar to send test messages, or Escape to exit.");
                Console.WriteLine("Press R to delete room, T to enter room, Y to delete current room, U to leave room.");
                Console.WriteLine("Press A to send chat message, S to check room list, D to check lobby user list, F to check current room user list, G to get user list in current room.");
                Console.WriteLine("Press H to enter lobby, or Escape to exit.");
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
                                // 룸 리스트 표시
                                foreach (var room in testServerSession.RoomManager.Rooms)
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

                            };

                            testServerSession.Send(c_DeleteRoom);
                            break;
                        }
                    case { Key: ConsoleKey.T }:
                        {
                            Console.WriteLine("\nT Key Pressed. Sending C_EnterRoom Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_EnterRoom c_EnterRoom = new C_EnterRoom();

                            Console.Write("Enter Room Id:");
                            string roomIdInput = Console.ReadLine();
                            if (int.TryParse(roomIdInput, out int roomId))
                            {
                                c_EnterRoom.RoomId = roomId;
                            }
                            else
                            {
                                Console.WriteLine("Invalid Room Id. Please enter a valid number.");
                                continue;
                            }

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {

                            };

                            testServerSession.Send(c_EnterRoom);
                            break;
                        }
                    case { Key: ConsoleKey.Y }:
                        {
                            Console.WriteLine("\nY Key Pressed. Sending C_EnterRoom Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_DeleteRoom c_deleteRoom = new C_DeleteRoom();

                            var currentRoom = serverSession.RoomManager.CurrentRoom;

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {
                                if (!serverSession.RoomManager.Rooms.ContainsKey(currentRoom.RoomId))
                                {
                                    Console.WriteLine($"{currentRoom.RoomName} 방 삭제 완료");
                                }
                            };

                            testServerSession.Send(c_deleteRoom);
                            break;
                        }
                    case { Key: ConsoleKey.U }:
                        {
                            Console.WriteLine("\nU Key Pressed. Sending C_LeaveRoom Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_LeaveRoom c_LeaveRoom = new C_LeaveRoom();

                            var currentRoom = serverSession.RoomManager.CurrentRoom;

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {

                            };

                            testServerSession.Send(c_LeaveRoom);
                            break;
                        }
                    case { Key: ConsoleKey.A }:
                        {
                            Console.WriteLine("\nA Key Pressed. Sending C_Chat Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_Chat c_Chat = new C_Chat();

                            Console.Write($"Input Message: ");
                            string message = Console.ReadLine();

                            c_Chat.Msg = message;

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {

                            };

                            testServerSession.Send(c_Chat);
                            break;
                        }
                    case { Key: ConsoleKey.S }:
                        {
                            Console.WriteLine("\nS Key Pressed. 패킷을 보내지 않고 현재 룸 리스트 확인");

                            RoomManager roomManager = serverSession.RoomManager;
                            if (roomManager.Rooms.Count == 0)
                            {
                                Console.WriteLine("No rooms available.");
                            }
                            else
                            {
                                Console.WriteLine("Available Rooms:");
                                foreach (var room in roomManager.Rooms)
                                {
                                    Console.WriteLine($"[{room.Key}] {room.Value.RoomName}");
                                }
                            }

                            break;
                        }
                    case { Key: ConsoleKey.D }:
                        {
                            Console.WriteLine("\nD Key Pressed. 패킷을 보내지 않고 현재 로비 유저 리스트 확인");

                            RoomManager roomManager = serverSession.RoomManager;
                            if (roomManager.UserInfos.Count == 0)
                            {
                                Console.WriteLine("No users available in the lobby.");
                            }
                            else
                            {
                                Console.WriteLine("Users in Lobby:");
                                foreach (var userInfo in roomManager.UserInfos)
                                {
                                    Console.WriteLine($"UserId: {userInfo.Key}, Nickname: {userInfo.Value.Nickname}");
                                }
                            }

                            break;
                        }
                    case { Key: ConsoleKey.F }:
                        {
                            Console.WriteLine("\nF Key Pressed. 패킷을 보내지 않고 현재 방 유저 리스트 확인");

                            RoomManager roomManager = serverSession.RoomManager;
                            if (roomManager.CurrentRoom == null)
                            {
                                // 로비 유저 리스트 출력
                                Console.WriteLine("You are not in any room. Here are the users in the lobby:");
                            }
                            else
                            {
                                Console.WriteLine($"Current Room: {roomManager.CurrentRoom.RoomName}");
                                Console.WriteLine("Users in Current Room:");
                                foreach (var user in roomManager.CurrentRoom.UserInfos.Values)
                                {
                                    Console.WriteLine($"UserId: {user}, Nickname: {user.Nickname}");
                                }
                            }

                            break;
                        }
                    case { Key: ConsoleKey.G }:
                        {
                            Console.WriteLine("\nG Key Pressed. Sending C_UserList Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_UserList c_UserList = new C_UserList();
                            c_UserList.RoomId = serverSession.RoomManager.CurrentRoom?.RoomId ?? 0;

                            RoomManager roomManager = serverSession.RoomManager;
                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {
                                if (c_UserList.RoomId == 0)
                                {
                                    Console.WriteLine("Users in Lobby:");
                                    foreach (var userInfo in roomManager.UserInfos)
                                    {
                                        Console.WriteLine($"UserId: {userInfo.Key}, Nickname: {userInfo.Value.Nickname}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Current Room: {roomManager.CurrentRoom.RoomName}");
                                    foreach (var user in roomManager.CurrentRoom.UserInfos.Values)
                                    {
                                        Console.WriteLine($"UserId: {user.UserId}, Nickname: {user.Nickname}");
                                    }
                                }
                            };

                            testServerSession.Send(c_UserList);

                            break;
                        }
                    case { Key: ConsoleKey.H }:
                        {
                            Console.WriteLine("\nH Key Pressed. Sending C_EnterLobby Messages...");

                            var testServerSession = serverSession as TestServerSession;
                            C_EnterLobby c_EnterLobby = new C_EnterLobby();

                            // 테스트 로그
                            testServerSession.testLog = () =>
                            {

                            };

                            testServerSession.Send(c_EnterLobby);

                            break;
                        }
                    case { Key: ConsoleKey.Escape }:
                        {
                            Console.WriteLine("\nEscape Key Pressed. Exiting...");
                            // 프로그램 종료
                            foreach (var session in sessions)
                            {
                                session.Disconnect();
                            }
                            Console.WriteLine("Disconnected from server. Exiting...");
                            Environment.Exit(0);
                            // Alternatively, you can use Environment.Exit(0) to exit the application immediately.
                            return;
                        }
                    default:
                        {
                            Console.WriteLine("\nPress Spacebar to send test messages or Escape to exit.");
                            break;
                        }
                }
            }
        }

        public static void DummyTest()
        {
            foreach (var session in sessions)
            {
                var dummySession = session as TestServerSession;
                C_EnterLobby c_EnterLobby = new C_EnterLobby();

                dummySession.Send(c_EnterLobby);
            }

            Thread.Sleep(1000);
            Console.WriteLine("DummyTest Start");
            // DummyTest는 세션을 생성하고, 로비에 입장한 후 랜덤으로 행동을 취함
            // 행동은 닉네임 변경, 방 생성, 방 입장, 채팅, 방 퇴장, 방 삭제 등
            // 행동은 1초 간격으로 진행됨
            while (true)
            {
                Thread.Sleep(testSendMs);

                foreach (var session in sessions)
                {
                    var dummySession = session as TestServerSession;
                    if (dummySession.CurrentState == UserState.Lobby)
                    {
                        var rnd = new Random();
                        int num = rnd.Next(0, dummySession.RoomManager.Rooms.Count > 0 ? 3 : 2);
                        if (num == 0)
                        {
                            //닉네임 변경
                            C_SetNickname c_SetNickname = new C_SetNickname();
                            c_SetNickname.Nickname = $"User_{rnd.Next()}";
                            dummySession.Send(c_SetNickname);
                        }
                        else if (num == 1)
                        {
                            //방 생성
                            C_CreateRoom c_CreateRoom = new C_CreateRoom();
                            c_CreateRoom.RoomName = $"TestRoom_{rnd.Next()}";
                            dummySession.Send(c_CreateRoom);
                        }
                        else
                        {
                            //방 입장
                            var room = dummySession.RoomManager.GetRandomRoomInfo();
                            if (room != null)
                            {
                                C_EnterRoom c_EnterRoom = new C_EnterRoom();
                                c_EnterRoom.RoomId = room.RoomId;
                                dummySession.Send(c_EnterRoom);
                            }
                            
                        }
                        //닉네임 변경
                        //방 생성(상태 변경 로비 -> 방)
                        //방이 있다면 방 입장(상태 변경 로비 -> 방)
                    }
                    else
                    {
                        //채팅
                        //방 퇴장(상태 변경 방->로비)
                        //방장이라면 방 삭제(상태 변경 방->로비)

                        // 유저 상태와 현재 방 접속 여부가 불일치하는 경우 서버에 새로운 유저 정보 요청
                        if (dummySession.RoomManager.CurrentRoom == null)
                        {
                            C_UserInfo c_UserInfo = new C_UserInfo();
                            dummySession.Send(c_UserInfo);
                            continue;
                        }

                        var rnd = new Random();
                        int num = rnd.Next(0, 2);
                        if (num == 0)
                        {
                            //채팅
                            C_Chat c_Chat = new C_Chat();
                            c_Chat.Msg = $"Test Message from {dummySession.UserInfo.Nickname}";
                            dummySession.Send(c_Chat);
                        }
                        else
                        {

                            //방장이라면 방 삭제
                            if (dummySession.RoomManager.CurrentRoom.RoomMasterUserId == dummySession.UserInfo.UserId)
                            {
                                C_DeleteRoom c_DeleteRoom = new C_DeleteRoom();
                                dummySession.Send(c_DeleteRoom);
                            }
                            else
                            {
                                //방 퇴장
                                C_LeaveRoom c_LeaveRoom = new C_LeaveRoom();
                                dummySession.Send(c_LeaveRoom);
                            }
                        }
                    }
                }
            }

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

                Thread.Sleep(testSendMs);
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

        StringBuilder testRoomIdlogs = new StringBuilder();
        object testRoomIdlogsLock = new object();

        public Action testLog;
        public TestServerSession(Socket socket) : base(socket)
        {
            testServerSessionName = $"TestSession_{Interlocked.Increment(ref count)}";
        }


        public new void Send(IMessage message)
        {
            Console.WriteLine($"{DateTime.UtcNow} {testServerSessionName}[{message.Descriptor.Name}]");
            base.Send(message);
        }

        public override void InitViewManager(IViewManager viewManager)
        {
            base.InitViewManager(viewManager);
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
            ushort size = BitConverter.ToUInt16(data.Array, 0);
            ushort packetId = BitConverter.ToUInt16(data.Array, 2);
            MsgId msgId = (MsgId)packetId;
            Console.WriteLine($"{DateTime.UtcNow} {testServerSessionName}[{msgId.ToString()}] size: {size}");

            PacketManager.Instance.InvokePacketHandler(this, data);

            if (msgId == MsgId.SEnterRoom)
            {
                ArraySegment<byte> d = new ArraySegment<byte>(data.Array, 4, size - 4);
                var s_EnterRoom = PacketManager.Instance.MakePacket<S_EnterRoom>(d);
                if (s_EnterRoom.ErrorCode == ErrorCode.Success)
                {
                    lock (testRoomIdlogsLock)
                    {
                        testRoomIdlogs.AppendLine($"{MsgId.SEnterRoom.ToString()} {s_EnterRoom.RoomInfo.RoomId} {CurrentState.ToString()} {RoomManager.CurrentRoom.RoomId} {RoomManager.CurrentRoom.RoomMasterUserId}");
                    }
                    
                }
            }
            else if (msgId == MsgId.SCreateRoom)
            {
                ArraySegment<byte> d = new ArraySegment<byte>(data.Array, 4, size - 4);
                var s_packet = PacketManager.Instance.MakePacket<S_CreateRoom>(d);
                if (s_packet.ErrorCode == ErrorCode.Success)
                {
                    lock (testRoomIdlogsLock)
                    {                        
                        testRoomIdlogs.AppendLine($"{MsgId.SCreateRoom.ToString()} {s_packet.RoomInfo.RoomId} {CurrentState.ToString()} {RoomManager.CurrentRoom?.RoomId} {RoomManager.CurrentRoom?.RoomMasterUserId}");
                    }
                }
            }
            else if (msgId == MsgId.SDeleteRoom)
            {
                ArraySegment<byte> d = new ArraySegment<byte>(data.Array, 4, size - 4);
                var s_packet = PacketManager.Instance.MakePacket<S_DeleteRoom>(d);
                if (s_packet.ErrorCode == ErrorCode.Success)
                {
                    lock (testRoomIdlogsLock)
                    {
                        testRoomIdlogs.AppendLine($"{MsgId.SDeleteRoom.ToString()} {CurrentState.ToString()}");
                    }
                }
            }
            else if (msgId == MsgId.SLeaveRoom)
            {
                ArraySegment<byte> d = new ArraySegment<byte>(data.Array, 4, size - 4);
                var s_packet = PacketManager.Instance.MakePacket<S_LeaveRoom>(d);
                if (s_packet.ErrorCode == ErrorCode.Success)
                {
                    lock (testRoomIdlogsLock)
                    {
                        testRoomIdlogs.AppendLine($"{MsgId.SLeaveRoom.ToString()} {CurrentState.ToString()}");
                    }
                }
            }
            else if (msgId == MsgId.SUserInfo)
            {
                ArraySegment<byte> d = new ArraySegment<byte>(data.Array, 4, size - 4);
                var s_packet = PacketManager.Instance.MakePacket<S_UserInfo>(d);
                lock (testRoomIdlogsLock)
                {
                    testRoomIdlogs.AppendLine($"{MsgId.SUserInfo.ToString()} RoomId: {s_packet.RoomInfo?.RoomId}, State: {s_packet.UserState}, RoomMasterId: {s_packet.RoomInfo?.RoomMasterUserId}");
                }
            }

                testLog?.Invoke();
        }

        public override void OnSend(int bytesTransferred)
        {
            //Console.WriteLine($"[{testServerSessionName}] Sent {bytesTransferred} bytes.");
        }
    }

}
