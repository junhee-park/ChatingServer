using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{

    public abstract class Session
    {

        public const int HEADER_SIZE = 2;


        Socket _socket;
        Queue<byte[]> _sendQueue = new Queue<byte[]>();

        public SocketAsyncEventArgs RecvArgs { get; }
        public SocketAsyncEventArgs SendArgs { get; }

        RecvBuffer _recvBuffer;
        public Socket Socket { get { return _socket; } }

        object _lock = new object();
        int _disconnect = 0;

        public abstract void OnRecv(byte[] data);
        public abstract void OnSend(int bytesTransferred);
        public abstract void OnConnect(EndPoint endPoint);
        public abstract void OnDisconnect(EndPoint endPoint);

        public Session(Socket socket)
        {
            this._socket = socket;
            this._recvBuffer = new RecvBuffer(1024);

            RecvArgs = new SocketAsyncEventArgs();
            RecvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(RecvCompleted);
            RecvArgs.SetBuffer(this._recvBuffer.Buffer);
            SendArgs = new SocketAsyncEventArgs();
            SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);

            ProcessRecv(RecvArgs);
        }


        public void ProcessRecv(SocketAsyncEventArgs args)
        {
            if (_disconnect == 1)
                return;

            this._recvBuffer.Clean();

            bool pending = _socket.ReceiveAsync(args);
            if (!pending)
                RecvCompleted(null, args);
        }

        void RecvCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                if (args.BytesTransferred < HEADER_SIZE)
                {
                    Console.WriteLine($"{args.BytesTransferred}");
                    return;
                }

                if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                {
                    Disconnect();
                    return;
                }

                ushort size = BitConverter.ToUInt16(this._recvBuffer.Buffer, 0);

                if (args.BytesTransferred < size)
                {
                    Console.WriteLine($"패킷 사이즈: {size}, 받은 바이트: {args.BytesTransferred}");
                    return;
                }

                ushort packetId = BitConverter.ToUInt16(this._recvBuffer.Buffer, HEADER_SIZE);

                // 뭔가 함
                OnRecv(this._recvBuffer.Buffer);

                if (_recvBuffer.OnRead(size) == false)
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

        public void RegisterSend(byte[] buffer)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(buffer);

            }
        }

        public void ProcessSend()
        {
            if (_disconnect == 1)
                return;

            lock ( _lock)
            {
                if (_sendQueue.Count == 0)
                    return;

                SendArgs.SetBuffer(_sendQueue.Dequeue());
            }
            bool pending = _socket.SendAsync(SendArgs);
            if (!pending)
                SendCompleted(null, SendArgs);

        }

        void SendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                OnSend(args.BytesTransferred);
                args.SetBuffer(null, 0, 0);

                // Console.WriteLine($"{args.BytesTransferred}");
            }
            else
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnect, 1) == 1)
                return;
            OnDisconnect(_socket.RemoteEndPoint);
            _sendQueue.Clear();
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
    }
}
