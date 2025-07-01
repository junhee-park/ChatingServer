using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;

public interface IViewManager
{
    void ShowText(string text);
    void ShowText(S_Chat s_Chat);
    void ShowRoomList(MapField<int, RoomInfo> roomInfos);
    void ShowRoomUserList(MapField<int, UserInfo> userInfos);
    void ShowLobbyUserList(MapField<int, UserInfo> userInfos);
    void ShowLobbyUserList(Dictionary<int, UserInfo> userInfos);
    void ShowChangedNickname(UserInfo userInfo, string newName);
    void ShowLobbyScreen();
    void ShowRoomScreen();
    void ShowAddedRoom(RoomInfo roomInfo);
    void ShowAddedUser(int roomId, UserInfo userInfo);
    void ShowRemovedUser(int roomId, UserInfo userInfo);
    void ShowRemovedRoom(int roomId);
}