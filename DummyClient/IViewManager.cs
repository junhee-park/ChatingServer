using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;

public interface IViewManager
{
    void ShowText(string text);
    void ShowText(S_Chat s_Chat);
    void ShowRoomList(RepeatedField<RoomInfo> roomInfos);
    void ShowRoomUserList(RepeatedField<UserInfo> userInfos);
    void ShowLobbyUserList(RepeatedField<UserInfo> userInfos);
    void ShowLobbyUserList(Dictionary<int, UserInfo> userInfos);
    void ShowChangedNickname(UserInfo userInfo, string newName);
    void ShowLobbyScreen();
    void ShowRoomScreen();
    void ShowAddedRoom(RoomInfo roomInfo);
    void ShowAddedUser(int roomId, UserInfo userInfo);
    void ShowRemovedUser(int roomId, UserInfo userInfo);
}