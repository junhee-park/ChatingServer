using System.Net;
using System.Net.Sockets;
using ServerCore;

namespace DummyClient
{
    internal class Program
    {
        static ServerSession serverSession;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, Client!");

            IPAddress[] iPAddress = Dns.GetHostAddresses(Dns.GetHostName());
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress[1], 7777);

            Connector connector = new Connector();
            connector.Connect(iPEndPoint,
                (saea) => {
                    serverSession = new ServerSession(saea.ConnectSocket);
                    return serverSession;
                },
                1);

            while (true)
            {
                if (serverSession == null)
                    continue;

                Thread.Sleep(100);

                string msg = Console.ReadLine();
                C_Chat c_Chat = new C_Chat();
                c_Chat.msg = msg;
                c_Chat.Write(out byte[] data);

                serverSession.SendArgs.SetBuffer(data);
                serverSession.ProcessSend();
            }
        }
    }

    public class ServerSession : Session
    {
        public ServerSession(Socket socket) : base(socket)
        {
        }

        public override void OnRecv(byte[] data)
        {
            ClientPacketManager.Instance.InvokePacketHandler(this, data);
        }
    }
}
