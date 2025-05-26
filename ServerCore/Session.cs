using System;
using System.Collections.Generic;
using System.Linq;
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

        byte[] _recvBuffer;
        public byte[] Buffer { get { return _recvBuffer; } }
        public int BufferSize { get { return _recvBuffer.Length; } }
        public Socket Socket { get { return _socket; } }

        object _lock = new object();

        public abstract void OnRecv(byte[] data);

        public Session(Socket socket)
        {
            this._socket = socket;
            this._recvBuffer = new byte[1024];

            RecvArgs = new SocketAsyncEventArgs();
            RecvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(RecvCompleted);
            RecvArgs.SetBuffer(this._recvBuffer);
            SendArgs = new SocketAsyncEventArgs();
            SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);

            ProcessRecv(RecvArgs);
        }


        public void ProcessRecv(SocketAsyncEventArgs args)
        {
            Array.Clear(this._recvBuffer);

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

                ushort size = BitConverter.ToUInt16(Buffer, 0);

                if (args.BytesTransferred < size)
                {
                    Console.WriteLine($"패킷 사이즈: {size}, 받은 바이트: {args.BytesTransferred}");
                    return;
                }

                ushort packetId = BitConverter.ToUInt16(Buffer, HEADER_SIZE);

                // TODO: 버퍼내 데이터와 헤더에 있는 사이즈 크기와 맞는지 확인 필요

                // 뭔가 함
                OnRecv(Buffer);

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
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
    }
}
