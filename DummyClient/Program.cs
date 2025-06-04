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
        static TestServerSession serverSession;
        static List<TestServerSession> sessions = new List<TestServerSession>();

        static object _lock = new object();

        // testConnection * (1000 / testSendMs) = tps
        static int testConnection = 10;
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
                        serverSession.OnConnect(saea.RemoteEndPoint);
                        sessions.Add(serverSession);
                        return serverSession;
                    }
                },
                testConnection);

            
            while (true)
            {
                Thread.Sleep(1000);

                if (sessions.Count != testConnection)
                    continue;

                break;
            }

            TestRtt();
            Console.ReadKey();
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
                    foreach (var session in sessions)
                    {
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
            C_Chat c_Chat = new C_Chat(); ;
            c_Chat.Msg = $"Test Message~~~~~~~";

            for (int i = 0; i < sessions.Count; i++)
            {
                TestServerSession session = sessions[i];

                c_Chat.TickCount = DateTime.UtcNow.Ticks;
                //c_Chat.TickCount = Environment.TickCount64;

                session.Send(c_Chat);
                session.ProcessSend();
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
        public TestServerSession(Socket socket) : base(socket)
        {
            testServerSessionName = $"TestSession_{Interlocked.Increment(ref count)}";
        }

        public void TestCompareRtt(S_Chat s_ChatPacket)
        {
            long rttMs = DateTime.UtcNow.Ticks - s_ChatPacket.TickCount;
            //long rttMs = Environment.TickCount64 - s_ChatPacket.TickCount;
            if (rttMs < minRttMs)
                minRttMs = rttMs;
            if (rttMs > maxRttMs)
                maxRttMs = rttMs;
            rtts.Add(rttMs);

            if (s_ChatPacket.UserId == 0)
                Console.WriteLine($"[{testServerSessionName} -> User_{s_ChatPacket.UserId}]: {s_ChatPacket.Msg}");

        }
    }

}
