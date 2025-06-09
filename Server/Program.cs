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
                
                Thread.Sleep(0);
            }
        }
    }
}
