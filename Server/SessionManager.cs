﻿using System;
using System.Collections.Concurrent;
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

        public ConcurrentDictionary<int, ClientSession> clientSessions = new ConcurrentDictionary<int, ClientSession>();
        int incSessionId = 0;

        static object _lock = new object();


        public ClientSession CreateSession(SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                ClientSession session = new ClientSession(args.AcceptSocket, incSessionId);
                clientSessions.TryAdd(incSessionId, session);
                session.OnConnect(session.Socket.RemoteEndPoint);

                incSessionId += 1;
                return session;
            }
        }

        public ClientSession GetClientSession(int userId)
        {
            lock (_lock)
            {
                if (clientSessions.TryGetValue(userId, out ClientSession session))
                    return session;
                return null;
            }
        }

        /// <summary>
        /// 모든 유저 세션에 메시지를 브로드캐스트합니다.
        /// </summary>
        /// <param name="data"></param>
        public void Boardcast(IMessage data)
        {
            foreach (var kvIdUser in clientSessions)
            {
                ClientSession session = kvIdUser.Value;

                session.Send(data);
            }
        }

        public void Disconnect(SocketAsyncEventArgs args)
        {
            ClientSession user = (ClientSession)args.UserToken;
            user.Disconnect();
            clientSessions.TryRemove(new KeyValuePair<int, ClientSession>(user.UserInfo.UserId, user));
        }

        public void RemoveSession(ClientSession clientSession)
        {
            lock (_lock)
            {
                clientSessions.TryRemove(new KeyValuePair<int, ClientSession>(clientSession.UserInfo.UserId, clientSession));
            }
        }
    }
}
