using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;

namespace Server
{
    internal class Program
    {
        static SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
        static Socket acceptSocket;

        static Dictionary<int, Session> sessions = new Dictionary<int, Session>();
        static int incSessionId = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, Server!");

            IPAddress[] iPAddress = Dns.GetHostAddresses(Dns.GetHostName());
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress[1], 7777);

            acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted);

            acceptSocket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            acceptSocket.Bind(iPEndPoint);
            acceptSocket.Listen(100);

            AcceptSocket();

            Console.ReadKey();
        }

        public static void AcceptSocket()
        {
            acceptArgs.AcceptSocket = null;

            bool isPending = acceptSocket.AcceptAsync(acceptArgs);
            if (!isPending)
                AcceptCompleted(null, acceptArgs);
        }

        public static void AcceptCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.AcceptSocket == null)
                return;

            ClientSession client = new ClientSession(args.AcceptSocket, incSessionId++);
            sessions.Add(client.UserId, client);

            AcceptSocket();
        }


        public static void Boardcast(byte[] data)
        {
            foreach(var kvIdUser in sessions)
            {
                Session session = kvIdUser.Value;
                SocketAsyncEventArgs sendArgs = kvIdUser.Value.SendArgs;
                sendArgs.SetBuffer(data);

                session.ProcessSend();
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
            UserId = userId;
        }

        public override void OnRecv(byte[] data)
        {
            ServerPacketManager.Instance.InvokePacketHandler(this, data);
        }
    }
}
