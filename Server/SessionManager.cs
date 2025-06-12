using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using ServerCore;

namespace Server
{
    public class SessionManager
    {
        #region Singleton
        static SessionManager _instance = new SessionManager();
        public static SessionManager Instance { get { return _instance; } }
        #endregion

        public Dictionary<int, Session> sessions = new Dictionary<int, Session>();
        int incSessionId = 0;

        static object _lock = new object();


        public Session CreateSession(SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                ClientSession session = new ClientSession(args.AcceptSocket, incSessionId);
                session.OnConnect(session.Socket.RemoteEndPoint);
                sessions.Add(incSessionId, session);

                incSessionId += 1;
                return session;
            }
        }

        /// <summary>
        /// 모든 유저 세션에 메시지를 브로드캐스트합니다.
        /// </summary>
        /// <param name="data"></param>
        public void Boardcast(IMessage data)
        {
            foreach (var kvIdUser in sessions)
            {
                ClientSession session = (ClientSession)kvIdUser.Value;

                session.Send(data);
            }
        }

        public void Disconnect(SocketAsyncEventArgs args)
        {
            ClientSession user = (ClientSession)args.UserToken;
            user.Disconnect();
            sessions.Remove(user.UserInfo.UserId);

        }
    }
}
