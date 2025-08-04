using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Server.Job;
using ServerCore;

namespace Server
{
    public class RoomManager : JobExecutor
    {
        #region Singleton
        static RoomManager _instance = new RoomManager();
        public static RoomManager Instance { get { return _instance; } }
        #endregion

        public int nextRoomId = 0; // Room ID를 생성하기 위한 카운터
        public ConcurrentDictionary<int, Room> rooms = new ConcurrentDictionary<int, Room>();
        public HashSet<int> userIds = new HashSet<int>(); // 로비에 존재하는 유저의 id목록

        public object _lock = new object();

        public Room CreateRoom(string roomName, int roomMasterId)
        {
            Room room = new Room(Interlocked.Increment(ref nextRoomId), roomName, roomMasterId);
            rooms.TryAdd(room.roomInfo.RoomId, room);
            return room;
        }

        public void CreateRoom(ClientSession clientSession, C_CreateRoom c_CreateRoomPacket)
        {
            // 유저 아이디 추출
            int userId = clientSession.UserInfo.UserId;
            // 패킷 생성
            S_CreateRoom s_CreateRoom = new S_CreateRoom();

            if (clientSession.CurrentState != UserState.Lobby)
            {
                Console.WriteLine($"{DateTime.UtcNow} [C_CreateRoomHandler] User {clientSession.UserInfo.UserId} is not in Lobby state.");
                s_CreateRoom.ErrorCode = ErrorCode.NotInLobby;
                s_CreateRoom.Reason = "You must be in the Lobby to create a room.";
                s_CreateRoom.UserState = clientSession.CurrentState;
                clientSession.Send(s_CreateRoom);
            }
            else
            {
                Room room = CreateRoom(c_CreateRoomPacket.RoomName, userId);
                room.AddUser(clientSession);
                RoomInfo roomInfo = room.roomInfo;
                s_CreateRoom.RoomInfo = roomInfo;
                s_CreateRoom.ErrorCode = ErrorCode.Success;

                LeaveUserFromLobby(userId); // 로비에서 유저 제거
                clientSession.CurrentState = UserState.Room;
                clientSession.Room = room; // 현재 방 정보 설정
                clientSession.Send(s_CreateRoom);

                S_CreateRoomBc s_CreateRoomBC = new S_CreateRoomBc();
                s_CreateRoomBC.RoomInfo = roomInfo;

                BroadcastToLobby(s_CreateRoom);

            }
        }

        public void EnterRoom(ClientSession clientSession, C_EnterRoom c_EnterRoomPacket)
        {
            // 유저 아이디 추출
            int userId = clientSession.UserInfo.UserId;
            // 패킷 생성
            S_EnterRoom s_EnterRoom = new S_EnterRoom();
            if (clientSession.CurrentState != UserState.Lobby)
            {
                Console.WriteLine($"{DateTime.UtcNow} [C_EnterRoomHandler] User {clientSession.UserInfo.UserId} is not in Lobby state.");
                s_EnterRoom.ErrorCode = ErrorCode.NotInLobby;
                s_EnterRoom.Reason = "You must be in the Lobby to enter a room.";
                s_EnterRoom.UserState = clientSession.CurrentState;
                clientSession.Send(s_EnterRoom);
                return;
            }
            if (!rooms.TryGetValue(c_EnterRoomPacket.RoomId, out Room room))
            {
                Console.WriteLine($"{DateTime.UtcNow} [C_EnterRoomHandler] Room {c_EnterRoomPacket.RoomId} does not exist.");
                s_EnterRoom.RoomInfo = new RoomInfo(); // 빈 방 정보 설정
                s_EnterRoom.RoomInfo.RoomId = c_EnterRoomPacket.RoomId; // 요청한 방 ID 설정
                s_EnterRoom.RoomInfo.RoomName = "Unknown Room"; // 방 이름 설정

                s_EnterRoom.ErrorCode = ErrorCode.RoomNotFound;
                s_EnterRoom.Reason = "The room you are trying to enter does not exist.";
                clientSession.Send(s_EnterRoom);
                return;
            }
            if (room.roomInfo.UserInfos.ContainsKey(userId))
            {
                Console.WriteLine($"{DateTime.UtcNow} [C_EnterRoomHandler] User {userId} is already in room {c_EnterRoomPacket.RoomId}.");
                s_EnterRoom.ErrorCode = ErrorCode.AlreadyInRoom;
                s_EnterRoom.Reason = "You are already in this room.";
                clientSession.Send(s_EnterRoom);
                return;
            }

            room.AddUser(clientSession);
            clientSession.CurrentState = UserState.Room;
            clientSession.Room = room; // 현재 방 정보 설정
            LeaveUserFromLobby(userId); // 로비에서 유저 제거
            s_EnterRoom.ErrorCode = ErrorCode.Success;
            s_EnterRoom.RoomInfo = room.roomInfo;

            // 룸 입장 응답 패킷 전송
            clientSession.Send(s_EnterRoom);

            // 룸에 있는 모든 유저에게 입장 알림 패킷 전송
            S_EnterRoomAnyUserBc s_EnterRoomAnyUser = new S_EnterRoomAnyUserBc();
            s_EnterRoomAnyUser.RoomId = room.roomInfo.RoomId;
            s_EnterRoomAnyUser.UserInfo = clientSession.UserInfo;
            room.Broadcast(s_EnterRoomAnyUser);

            // 로비에 있는 유저들에게 룸 입장 알림 패킷 전송
            BroadcastToLobby(s_EnterRoomAnyUser);
        }

        public Room GetRoom(int roomId)
        {
            if (rooms.TryGetValue(roomId, out Room room))
                return room;
            return null;
        }

        public void GetLobbyUsers(in MapField<int, UserInfo> userInfos)
        {
            lock (_lock)
            {
                foreach (var userId in userIds)
                {
                    if (SessionManager.Instance.clientSessions.TryGetValue(userId, out ClientSession clientSession))
                    {
                        userInfos.Add(userId, clientSession.UserInfo);
                    }
                }
            }
        }

        public bool RemoveRoom(int roomId, int masterUserId)
        {
            // 방 삭제
            if (!rooms.TryGetValue(roomId, out Room room))
                return false;
            if (room.roomInfo.RoomMasterUserId != masterUserId)
                return false; // 방장만 방을 삭제할 수 있음
            return rooms.TryRemove(new KeyValuePair<int, Room>(roomId, room));
        }

        public void RemoveRoom(ClientSession clientSession)
        {
            // 패킷 생성
            S_DeleteRoom s_DeleteRoom = new S_DeleteRoom();

            Room clientSessionCurrentRoom = clientSession.Room;
            if (clientSessionCurrentRoom == null)
            {
                Console.WriteLine($"{DateTime.UtcNow} [C_DeleteRoomHandler] User {clientSession.UserInfo.UserId} is not in a room.");
                s_DeleteRoom.ErrorCode = ErrorCode.NotInRoom;
                s_DeleteRoom.Reason = "You are not in a room.";
                s_DeleteRoom.UserState = clientSession.CurrentState;
                clientSession.Send(s_DeleteRoom);
                return;
            }

            S_DeleteRoomBc s_DeleteRoomBC = new S_DeleteRoomBc();
            foreach (var room in rooms.Values)
            {
                s_DeleteRoomBC.Rooms.Add(room.roomInfo.RoomId, room.roomInfo);
            }
            foreach (var userId in userIds)
            {
                if (SessionManager.Instance.clientSessions.TryGetValue(userId, out ClientSession clientSessionInLobby))
                {
                    s_DeleteRoomBC.LobbyUserInfos.Add(userId, clientSessionInLobby.UserInfo);
                }
            }

            bool success = RemoveRoom(clientSession.Room.roomInfo.RoomId, clientSession.UserInfo.UserId);
            s_DeleteRoom.ErrorCode = success ? ErrorCode.Success : ErrorCode.NotAuthorized;
            if (success)
            {
                S_DeleteAnyRoomInLobbyBc s_DeleteAnyRoomInLobby = new S_DeleteAnyRoomInLobbyBc();
                s_DeleteAnyRoomInLobby.RoomId = clientSession.Room.roomInfo.RoomId;

                // 방에 있는 모든 유저들을 로비로 이동
                foreach (var userInfo in clientSession.Room.roomInfo.UserInfos.Values)
                {
                    SessionManager.Instance.clientSessions.TryGetValue(userInfo.UserId, out ClientSession cs);
                    // 유저 상태 변경
                    cs.CurrentState = UserState.Lobby;
                    cs.Room = null;
                    AddUserToLobby(userInfo.UserId);

                    s_DeleteAnyRoomInLobby.UserInfos.Add(userInfo);
                }

                s_DeleteRoom.UserState = clientSession.CurrentState;
                clientSession.Send(s_DeleteRoom); // 방 삭제 성공 시 클라이언트에게 전송

                clientSessionCurrentRoom.Broadcast(s_DeleteRoomBC);

                // 로비에 있는 유저들에게 삭제되는 방 정보와 로비에 추가될 유저 정보 전송
                BroadcastToLobby(s_DeleteAnyRoomInLobby);
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow} [C_DeleteRoomHandler] User {clientSession.UserInfo.UserId} failed to delete room {clientSession.Room.roomInfo.RoomId}.");
                s_DeleteRoom.Reason = "Failed to delete room. You must be the room master.";
                s_DeleteRoom.UserState = clientSession.CurrentState;
                clientSession.Send(s_DeleteRoom);
            }
        }

        public void AddUserToRoom(int roomId, ClientSession session)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                room.AddUser(session);
            }
        }

        public void AddUserToLobby(int userId)
        {
            lock (_lock)
            {
                userIds.Add(userId); // 로비에 유저 추가
            }
        }

        public bool LeaveUserFromRoom(int roomId, int userId)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                return room.LeaveUser(userId);
            }
            return false;
        }

        public bool LeaveUserFromLobby(int userId)
        {
            lock (_lock)
            {
                return userIds.Remove(userId); // 로비에서 유저 제거
            }
        }

        public void LeaveRoom(ClientSession clientSession)
        {
            S_LeaveRoom s_LeaveRoom = new S_LeaveRoom();

            Room room = clientSession.Room; // 현재 방 정보
            if (room == null)
                return;

            LeaveUserFromRoom(clientSession.Room.roomInfo.RoomId, clientSession.UserInfo.UserId);
            clientSession.CurrentState = UserState.Lobby; // 현재 상태를 Lobby로 변경
            clientSession.Room = null;

            // 퇴장하는 유저에게 방 목록과 로비 유저 리스트 전송
            foreach (var item in rooms)
            {
                s_LeaveRoom.Rooms.Add(item.Key, item.Value.roomInfo);
            }

            foreach (var userId in userIds)
            {
                if (SessionManager.Instance.clientSessions.TryGetValue(userId, out ClientSession lobbyClientSession))
                {
                    s_LeaveRoom.UserInfos.Add(userId, lobbyClientSession.UserInfo);
                }
            }

            // 패킷 생성
            S_LeaveRoomAnyUserBc s_LeaveRoomAnyUser = new S_LeaveRoomAnyUserBc();
            s_LeaveRoomAnyUser.RoomId = room.roomInfo.RoomId;
            s_LeaveRoomAnyUser.UserInfo = clientSession.UserInfo;

            // 룸에 있는 모든 유저에게 퇴장 알림 패킷 전송
            room.Broadcast(s_LeaveRoomAnyUser);

            // 로비에 있는 유저들에게 로비 입장 패킷 전송
            BroadcastToLobby(s_LeaveRoomAnyUser);

            clientSession.Send(s_LeaveRoom);

            // 서버 로비에 퇴장하는 유저 추가
            AddUserToLobby(clientSession.UserInfo.UserId);

        }

        public void EnterLobby(ClientSession clientSession)
        {
            // 현재 상태를 로비로 변경
            clientSession.CurrentState = UserState.Lobby;
            clientSession.Room = null; // 현재 방 정보 초기화

            // 패킷 생성
            S_EnterLobby s_EnterLobby = new S_EnterLobby();
            s_EnterLobby.UserInfo = clientSession.UserInfo;

            // 룸 리스트
            foreach (var room in rooms.Values)
            {
                s_EnterLobby.Rooms.Add(room.roomInfo.RoomId, room.roomInfo);
            }

            // 유저 리스트
            foreach (int userId in userIds)
            {
                ClientSession userSession = SessionManager.Instance.GetClientSession(userId);
                if (userSession != null)
                {
                    s_EnterLobby.UserInfos.Add(userId, userSession.UserInfo);
                }
            }

            // 로비에 접속한 유저에게 로비 정보 전송
            clientSession.Send(s_EnterLobby);

            // 로비에 있는 유저들에게 접속 알림 패킷 전송
            S_EnterLobbyAnyUserBc s_EnterLobbyAnyUser = new S_EnterLobbyAnyUserBc();
            s_EnterLobbyAnyUser.UserInfo = clientSession.UserInfo;
            BroadcastToLobby(s_EnterLobbyAnyUser);

            // 로비에 있는 유저 리스트에 추가
            AddUserToLobby(clientSession.UserInfo.UserId);
        }

        public void SetNickname(ClientSession clientSession, C_SetNickname c_SetNicknamePacket)
        {
            // TODO: 닉네임 중복 체크 로직 추가 필요

            // 로비에서만 닉네임 설정 가능
            S_SetNickname s_SetNickname = new S_SetNickname();
            if (clientSession.CurrentState != UserState.Lobby)
            {
                Console.WriteLine($"[C_SetNicknameHandler] User {clientSession.UserInfo.UserId} is not in Lobby state.");
                s_SetNickname.ErrorCode = ErrorCode.NotInLobby;
                s_SetNickname.Reason = "You must be in the Lobby to set a nickname.";
                s_SetNickname.UserState = clientSession.CurrentState;
                clientSession.Send(s_SetNickname);
                return;
            }

            clientSession.UserInfo.Nickname = c_SetNicknamePacket.Nickname;
            s_SetNickname.ErrorCode = ErrorCode.Success;
            clientSession.Send(s_SetNickname);

            S_SetNicknameBc s_SetNicknameBC = new S_SetNicknameBc();
            s_SetNicknameBC.UserId = clientSession.UserInfo.UserId;
            s_SetNicknameBC.Nickname = c_SetNicknamePacket.Nickname;
            BroadcastToLobby(s_SetNickname);
        }

        public void GetUserInfo(ClientSession clientSession)
        {
            S_UserInfo s_UserInfo = new S_UserInfo();
            s_UserInfo.UserInfo = clientSession.UserInfo;
            s_UserInfo.UserState = clientSession.CurrentState;
            if (clientSession.CurrentState == UserState.Room)
            {
                s_UserInfo.RoomInfo = new RoomInfo();
                s_UserInfo.RoomInfo.MergeFrom(clientSession.Room.roomInfo);
            }

            clientSession.Send(s_UserInfo);
        }

        public void DisconnectUser(ClientSession clientSession)
        {
            // 방에 참여 중인 경우 방에서 유저 제거
            if (clientSession.Room != null)
            {
                if (clientSession.Room.roomInfo.RoomMasterUserId == clientSession.UserInfo.UserId)
                {
                    // 방장인 경우
                    clientSession.Room.LeaveUser(clientSession.UserInfo.UserId); // 방장 유저 제거
                    PacketHandler.C_DeleteRoomHandler(clientSession, new C_DeleteRoom());
                }
                else
                {
                    // 일반 유저인 경우
                    clientSession.Room.LeaveUser(clientSession.UserInfo.UserId);
                    clientSession.Room.Broadcast(new S_LeaveRoomAnyUserBc() { RoomId = clientSession.Room.roomInfo.RoomId, UserInfo = clientSession.UserInfo }); // 방에 있는 모든 유저에게 알림
                    clientSession.Room = null; // 방에서 나감
                }
            }

            // 로비에 있는 경우 로비에서 유저 제거
            if (clientSession.CurrentState == UserState.Lobby)
            {
                LeaveUserFromLobby(clientSession.UserInfo.UserId);
                BroadcastToLobby(new S_LeaveLobbyAnyUserBc() { UserInfo = clientSession.UserInfo }); // 로비에 있는 모든 유저에게 알림
            }
            // 세션 종료
            SessionManager.Instance.RemoveSession(clientSession);
        }

        public void BroadcastToLobby(IMessage message)
        {
            lock ( _lock)
            {
                foreach (var userId in userIds)
                {
                    if (SessionManager.Instance.clientSessions.TryGetValue(userId, out ClientSession clientSession))
                    {
                        clientSession.Send(message);
                    }
                }
            }
        }
    }
}
