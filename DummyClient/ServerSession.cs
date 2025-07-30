using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using ServerCore;
using Google.Protobuf.Protocol;
using System.Globalization;
public class ServerSession : PacketSession
{
    public UserInfo UserInfo { get; set; } = new UserInfo(); // 유저 정보
    public IViewManager ViewManager { get; set; } // 뷰 매니저

    public RoomManager RoomManager { get; set; } = new RoomManager(); // 방 관리

    private UserState _currentState;
    public UserState CurrentState
    {
        get => _currentState;
        set
        {
            _currentState = value;
            if (_currentState == UserState.None)
                throw new InvalidOperationException("UserState cannot be None.");
            ViewManager.ShowChangedScreen(_currentState);
        }
    }
    public ServerSession(Socket socket) : base(socket)
    {

    }

    public virtual void InitViewManager(IViewManager viewManager)
    {
        ViewManager = viewManager;
    }

    public void Send(IMessage message)
    {
        string packetName = message.Descriptor.Name.Replace("_", string.Empty);
        MsgId packetId = (MsgId)System.Enum.Parse(typeof(MsgId), packetName);
        int packetSize = message.CalculateSize();
        ArraySegment<byte> segment = new ArraySegment<byte>(new byte[packetSize + 4]);
        BitConverter.TryWriteBytes(segment.Array, (ushort)(packetSize + 4));
        BitConverter.TryWriteBytes(new ArraySegment<byte>(segment.Array, 2, segment.Count - 2), (ushort)packetId);
        Array.Copy(message.ToByteArray(), 0, segment.Array, 4, packetSize);

        RegisterSend(segment.Array);
    }

    public override void OnConnect(EndPoint endPoint)
    {

    }

    public override void OnDisconnect(EndPoint endPoint)
    {

    }

    public override void OnRecvPacket(ArraySegment<byte> data)
    {
        PacketManager.Instance.InvokePacketHandler(this, data);
    }

    public override void OnSend(int bytesTransferred)
    {

    }
}