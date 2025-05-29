using System.Net;
using System.Net.Sockets;
using ServerCore;

namespace DummyClient
{
    internal class Program
    {
        static ServerSession serverSession;
        static List<ServerSession> sessions = new List<ServerSession>();

        static object _lock = new object();

        static int testConnection = 100;



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
                        serverSession = new ServerSession(saea.ConnectSocket);
                        serverSession.OnConnect(saea.RemoteEndPoint);
                        sessions.Add(serverSession);
                        return serverSession;
                    }
                },
                testConnection);

            while (true)
            {
                if (sessions.Count != testConnection)
                    continue;

                Thread.Sleep(100);

                Testing();
                //string msg = Console.ReadLine();
                //C_Chat c_Chat = new C_Chat();
                //c_Chat.msg = msg;
                //c_Chat.Write(out byte[] data);

                //serverSession.SendArgs.SetBuffer(data);
                //serverSession.ProcessSend();
            }
            //Console.ReadKey();
        }

        public static void Testing()
        {
            C_Chat c_Chat = new C_Chat(); ;
            c_Chat.msg = $"Test Message";
            c_Chat.Write(out byte[] data);

            for (int i = 0; i < sessions.Count; i++)
            {
                ServerSession session = sessions[i];

                session.RegisterSend(data);
                session.ProcessSend();
                //for (int i = 0; i < 100; i++)
                //{
                //    C_Chat c_Chat = new C_Chat(); ;
                //    c_Chat.msg = $"Test Message {i}";
                //    c_Chat.Write(out byte[] data);
                //    session.SendArgs.SetBuffer(data);
                //    session.ProcessSend();
                //    Thread.Sleep(100);
                //}
            }
        }
    }

    public class ServerSession : PacketSession
    {
        public static int count = 0;
        public string testServerSessionName;
        public ServerSession(Socket socket) : base(socket)
        {
            testServerSessionName = $"TestSession_{Interlocked.Increment(ref count)}";
        }

        public override void OnConnect(EndPoint endPoint)
        {

        }

        public override void OnDisconnect(EndPoint endPoint)
        {

        }

        public override void OnRecvPacket(ArraySegment<byte> data)
        {
            ClientPacketManager.Instance.InvokePacketHandler(this, data);
        }

        public override void OnSend(int bytesTransferred)
        {

        }
    }
}
