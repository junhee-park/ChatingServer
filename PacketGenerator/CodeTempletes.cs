using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator
{
    internal class CodeTempletes
    {
        /*
         * {0} 패킷 핸들러와 패킷 생성 메소드들을 추가하는 부분
         */
        public const string PacketManager = @"
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

public class PacketManager
{{
    #region Singleton
    static PacketManager _instance = new PacketManager();
    public static PacketManager Instance {{ get {{ return _instance; }} }}
    #endregion

    Dictionary<ushort, Action<Session, IMessage>> _handlers = new Dictionary<ushort, Action<Session, IMessage>> ();
    Dictionary<ushort, Func<ArraySegment<byte>, IMessage>> _makePacket = new Dictionary<ushort, Func<ArraySegment<byte>, IMessage>>();
        
    public PacketManager()
    {{
{0}
    }}

    public T MakePacket<T>(ArraySegment<byte> buffer) where T : IMessage, new()
    {{
        T packet = new T();
        packet.MergeFrom(buffer);

        return packet;
    }}

    public void InvokePacketHandler(Session session, ArraySegment<byte> buffer)
    {{
        ushort size = BitConverter.ToUInt16(buffer.Array, 0);
        ushort packetId = BitConverter.ToUInt16(buffer.Array, 2);

        ArraySegment<byte> data = new ArraySegment<byte>(buffer.Array, 4, size - 4);
        bool result = _makePacket.TryGetValue(packetId, out var makePacketFunc);
        if (!result)
        {{
            return;
        }}
        IMessage packet = makePacketFunc.Invoke(data);

        result = _handlers.TryGetValue(packetId, out var handler);
        if (!result)
        {{
            return;
        }}
        handler?.Invoke(session, packet);
    }}
}}
";
        /*
         * {0} Protocol파일에 정의된 메세지 아이디
         * {1} 핸들러 및 패킷 클래스 이름
         */
        public const string PacketHandler =
@"        _handlers.Add((ushort)MsgId.{0}, PacketHandler.{1}Handler);
        _makePacket.Add((ushort)MsgId.{0}, MakePacket<{1}>);";
    }
}
