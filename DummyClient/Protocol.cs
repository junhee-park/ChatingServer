using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public enum PacketId
{
    S_CHAT = 0,
    C_CHAT = 1,
}

public abstract class Packet
{
    public ushort size;
    public ushort packetId;

    public virtual void Read(byte[] data)
    {
        size = BitConverter.ToUInt16(data, 0);
        packetId = BitConverter.ToUInt16(data, 2);
    }
}

public class S_Chat : Packet
{
    public int userId;
    public string msg;

    public override void Read(byte[] data)
    {
        base.Read(data);
        int count = 4;
        userId = BitConverter.ToInt32(data, count);
        count += sizeof(int);
        msg = Encoding.UTF8.GetString(data, count, size);
    }

    public bool Write(out byte[] buffer)
    {
        bool result = false;
        int offset = 2;
        int msgSize = Encoding.UTF8.GetByteCount(msg);
        buffer = new byte[2 + sizeof(int) + msgSize + 2];
        result = BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, sizeof(ushort)), (ushort)PacketId.S_CHAT);
        offset += 2;

        result = BitConverter.TryWriteBytes(new Span<byte>(buffer, offset, sizeof(int)), userId);
        offset += sizeof(int);
        offset += Encoding.UTF8.GetBytes(msg, new Span<byte>(buffer, offset, msgSize));

        // 패킷 사이즈
        result = BitConverter.TryWriteBytes(new Span<byte>(buffer, 0, sizeof(ushort)), (ushort)offset);
        size = (ushort)offset;

        return result;
    }
}

public class C_Chat : Packet
{
    public string msg;

    public override void Read(byte[] data)
    {
        base.Read(data);
        int count = 4;
        msg = Encoding.UTF8.GetString(data, count, size);
    }

    public bool Write(out byte[] buffer)
    {
        bool result = false;
        int count = 2;
        int msgSize = Encoding.UTF8.GetByteCount(msg);
        buffer = new byte[2 + sizeof(int) + msgSize + 2];
        result = BitConverter.TryWriteBytes(new Span<byte>(buffer, count, sizeof(ushort)), (ushort)PacketId.C_CHAT);
        count += 2;

        count += Encoding.UTF8.GetBytes(msg, new Span<byte>(buffer, count, msgSize));

        // 패킷 사이즈
        result = BitConverter.TryWriteBytes(new Span<byte>(buffer, 0, sizeof(ushort)), (ushort)count);
        size = (ushort)count;

        return result;
    }
}