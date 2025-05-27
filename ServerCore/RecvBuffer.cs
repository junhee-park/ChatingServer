using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{

    public class RecvBuffer
    {
        byte[] _recvBuffer;
        public byte[] Buffer { get { return _recvBuffer; } }
        public int BufferSize { get { return _recvBuffer.Length; } }
        public int ReadPos { get; set; }
        public int WritePos { get; set; }

        public int DataSize { get { return WritePos - ReadPos; } }
        public int FreeSize { get { return _recvBuffer.Length - DataSize; } }

        public RecvBuffer(int bufferSize)
        {
            _recvBuffer = new byte[bufferSize];
        }

        public void Clean()
        {
            if (DataSize == 0)
                ReadPos = WritePos = 0;
            else
            {
                Array.Copy(_recvBuffer, ReadPos, _recvBuffer, 0, DataSize);
                WritePos = DataSize - ReadPos;
                ReadPos = 0;

            }
        }

        public bool OnRead(int readBytes)
        {
            if (readBytes > FreeSize)
                return false;

            ReadPos += readBytes;
            return true;
        }

        public bool OnWrite(int writeBytes)
        {
            if (writeBytes > FreeSize)
                return false;

            WritePos += writeBytes;
            return true;
        }
    }
}
