using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Listener
    {
        Socket _listener;
        Func<SocketAsyncEventArgs, Session> _sessionFactory;

        public void Init(IPEndPoint iPEndPoint, Func<SocketAsyncEventArgs, Session> sessionFactory, int register = 10, int backlog = 100)
        {
            _sessionFactory += sessionFactory;
            _listener = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(iPEndPoint);
            _listener.Listen(backlog);

            for (int i = 0; i < register; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted);

                RegisterAccept(args);
            }
        }

        public void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool pending = _listener.AcceptAsync(args);
            if (!pending)
                AcceptCompleted(null, args);
        }

        public void AcceptCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                _sessionFactory.Invoke(args);
            }
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }

            RegisterAccept(args);
        }
    }
}
