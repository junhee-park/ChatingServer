using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Connector
    {
        Func<SocketAsyncEventArgs, Session> _sessionFactory;
        public void Connect(IPEndPoint iPEndPoint, Func<SocketAsyncEventArgs, Session> func, int connectorCount = 1)
        {
            _sessionFactory = func;

            for (int i = 0; i < connectorCount; i++)
            {
                Socket socket = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
                saea.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectCompleted);
                saea.RemoteEndPoint = iPEndPoint;
                saea.UserToken = socket;

                ProcessConnect(saea);

                // TEMP
                Thread.Sleep(10);
            }
        }

        public void ProcessConnect(SocketAsyncEventArgs saea)
        {
            Socket socket = (Socket)saea.UserToken;

            bool pending = socket.ConnectAsync(saea);
            if (!pending)
                ConnectCompleted(null, saea);
        }

        public void ConnectCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke(e);
            }
            else
            {
                Console.WriteLine(e.SocketError.ToString());
            }
        }
    }
}
