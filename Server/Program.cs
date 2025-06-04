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
                        session.OnConnect(iPEndPoint);
                        sessions.Add(incSessionId, session);

                        incSessionId += 1;
                        return session;
                    }
                });

            while (true)
            {
                
                Thread.Sleep(0);
            }
        }

        public static void Boardcast(IMessage data)
        {
            foreach(var kvIdUser in sessions)
            {
                ClientSession session = (ClientSession)kvIdUser.Value;

                session.Send(data);
            }
        }

        public static void Disconnect(SocketAsyncEventArgs args)
        {
            ClientSession user = (ClientSession)args.UserToken;
            user.Disconnect();
            sessions.Remove(user.UserId);

        }
    }

}
