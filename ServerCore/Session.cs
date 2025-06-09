using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        protected PacketSession(Socket socket) : base(socket)
        {
        }

        public sealed override int OnRecv(ArraySegment<byte> data)
        {
            int processLen = 0;

            // 한 패킷에 여러 메세지가 이어져서 들어올 경우 처리할 수 있을 때까지 처리
            while (true)
            {
                if (data.Count < HEADER_SIZE)
                {
                    break;
                }
                ushort size = BitConverter.ToUInt16(data.Array, data.Offset);
                if (data.Count < size)
                {
                    break;
                }

                OnRecvPacket(new ArraySegment<byte>(data.Array, data.Offset, size));

                processLen += size;
                data = new ArraySegment<byte>(data.Array, data.Offset + size, data.Count - size);
            }

            return processLen;
        }

        /// <summary>
        /// 패킷 조립 및 핸들러 실행
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }
    public abstract class Session
    {

        public const int HEADER_SIZE = 2;

        Socket _socket;
        Queue<byte[]> _sendQueue = new Queue<byte[]>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        public SocketAsyncEventArgs RecvArgs { get; }
        public SocketAsyncEventArgs SendArgs { get; }

        RecvBuffer _recvBuffer;
        public Socket Socket { get { return _socket; } }

        object _lock = new object();
        int _disconnect = 0;

        public abstract int OnRecv(ArraySegment<byte> data);
        public abstract void OnSend(int bytesTransferred);
        public abstract void OnConnect(EndPoint endPoint);
        public abstract void OnDisconnect(EndPoint endPoint);

        public Session(Socket socket)
        {
            this._socket = socket;
            this._recvBuffer = new RecvBuffer(65565);

            RecvArgs = new SocketAsyncEventArgs();
            RecvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(RecvCompleted);
            SendArgs = new SocketAsyncEventArgs();
            SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);

            ProcessRecv(RecvArgs);
        }


        void ProcessRecv(SocketAsyncEventArgs args)
        {
            if (_disconnect == 1)
                return;

            _recvBuffer.Clean();
            RecvArgs.SetBuffer(this._recvBuffer.WriteSegment);

            bool pending = _socket.ReceiveAsync(args);
            if (!pending)
                RecvCompleted(null, args);
        }

        void RecvCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                // 리시브 버퍼의 읽기 포지션을 전송받은 바이트만큼 이동
                if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                {
                    Disconnect();
                    return;
                }

                // 패킷 조립 및 패킷과 대응되는 핸들러 실행
                int processLen = OnRecv(this._recvBuffer.ReadSegment);

                // 리시브 버퍼의 쓰기 포시션을 패킷 조립이 끝난만큼 이동
                if (_recvBuffer.OnRead(processLen) == false)
                {
                    Disconnect();
                    return;
                }

                ProcessRecv(args);
            }
            else
            {
                Disconnect();
            }
        }

        protected void RegisterSend(byte[] buffer)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(buffer);

                if (_pendingList.Count == 0)
                    ProcessSend();
            }
        }

        protected void ProcessSend()
        {
            if (_disconnect == 1)
                return;

            lock ( _lock)
            {
                if (_sendQueue.Count == 0)
                    return;

                while (_sendQueue.Count > 0)
                    _pendingList.Add(_sendQueue.Dequeue());

                SendArgs.BufferList = _pendingList;

                bool pending = _socket.SendAsync(SendArgs);
                if (!pending)
                    SendCompleted(null, SendArgs);
            }

        }

        void SendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    OnSend(args.BytesTransferred);
                    
                    SendArgs.BufferList = null;
                    _pendingList.Clear();

                    if (_sendQueue.Count > 0)
                        ProcessSend();
                }
                else
                {
                    Disconnect();
                }
            }
        }
        void Clear()
        {
            lock (_lock)
            {
                _sendQueue.Clear();
                _pendingList.Clear();
            }
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnect, 1) == 1)
                return;
            OnDisconnect(_socket.RemoteEndPoint);
            Clear();
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
    }
}
