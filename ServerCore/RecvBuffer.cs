using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{

    public class RecvBuffer
    {
        ArraySegment<byte> _recvBuffer;
        public ArraySegment<byte> Buffer { get { return _recvBuffer; } }
        int _readPos;
        int _writePos;

        public int DataSize { get { return _writePos - _readPos; } }
        public int FreeSize { get { return _recvBuffer.Count - _writePos; } }
        public ArraySegment<byte> ReadSegment { get { return new ArraySegment<byte>(_recvBuffer.Array, _recvBuffer.Offset + _readPos, DataSize); } }
        public ArraySegment<byte> WriteSegment { get { return new ArraySegment<byte>(_recvBuffer.Array, _recvBuffer.Offset + _writePos, FreeSize); } }

        public RecvBuffer(int bufferSize)
        {
            _recvBuffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public void Clean()
        {
            if (DataSize == 0)
                _readPos = _writePos = 0;
            else
            {
                Array.Copy(_recvBuffer.Array, _recvBuffer.Offset + _readPos, _recvBuffer.Array, _recvBuffer.Offset, DataSize);
                _writePos = DataSize;
                _readPos = 0;

            }
        }

        public bool OnRead(int readBytes)
        {
            if (readBytes > DataSize)
                return false;

            _readPos += readBytes;
            return true;
        }

        public bool OnWrite(int writeBytes)
        {
            if (writeBytes > FreeSize)
                return false;

            _writePos += writeBytes;
            return true;
        }
    }
}
