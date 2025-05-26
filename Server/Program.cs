using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server
{
    internal class Program
    {
        public static Dictionary<int, Session> sessions = new Dictionary<int, Session>();
        static int incSessionId = 0;

        static object _lock = new object();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, Server!");

            IPAddress[] iPAddress = Dns.GetHostAddresses(Dns.GetHostName());
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress[1], 7777);

            Listener listener = new Listener();
            listener.Init(iPEndPoint,
                (args) =>
                {
                    lock (_lock)
                    {
                        ClientSession session = new ClientSession(args.AcceptSocket, incSessionId);
                        sessions.Add(incSessionId, session);
                        incSessionId += 1;
                        return session;
                    }
                });

            while (true)
            {
                lock (_lock)
                {
                    foreach (var kvIdUser in sessions)
                    {
                        Session session = kvIdUser.Value;

                        session.ProcessSend();
                    }
                }
                Thread.Sleep(100);
            }
        }

        public static void Boardcast(byte[] data)
        {
            foreach(var kvIdUser in sessions)
            {
                Session session = kvIdUser.Value;

                session.RegisterSend(data);
            }
        }

        public static void Disconnect(SocketAsyncEventArgs args)
        {
            ClientSession user = (ClientSession)args.UserToken;
            user.Disconnect();
            sessions.Remove(user.UserId);

        }
    }

    public class ClientSession : Session
    {
        public int UserId { get; set; }
        public ClientSession(Socket socket, int userId) : base(socket)
        {
            Console.WriteLine($"생성자 {userId}");
            UserId = userId;
        }

        public override void OnRecv(byte[] data)
        {
            Console.WriteLine($"OnRecv {UserId}");
            ServerPacketManager.Instance.InvokePacketHandler(this, data);
        }
    }
}
