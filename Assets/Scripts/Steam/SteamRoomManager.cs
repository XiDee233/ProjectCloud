using UnityEngine;
using PurrNet;
using PurrNet.Steam;
using System.Collections.Generic;
using System;

namespace Steam
{
    /// <summary>
    /// Steam房间系统，实现steam连接、开房间和选择房间加入功能
    /// </summary>
    public class SteamRoomManager : MonoBehaviour
    {
        // 房间信息类
        [Serializable]
        public class SteamRoomInfo
        {
            public string RoomId;
            public string RoomName;
            public int PlayerCount;
            public int MaxPlayers;
            public string HostName;
        }

        // 事件
        public event Action<bool> onSteamConnected;
        public event Action<List<SteamRoomInfo>> onRoomListUpdated;
        public event Action<bool> onRoomCreated;
        public event Action<bool> onRoomJoined;

        // 核心引用
        private NetworkManager _networkManager;
        private SteamTransport _steamTransport;

        // 房间列表
        [SerializeField]
        private List<SteamRoomInfo> _roomList = new List<SteamRoomInfo>();

        // Steam API回调
        private Steamworks.Callback<Steamworks.LobbyMatchList_t> _lobbyMatchListCallback;
        private Steamworks.Callback<Steamworks.LobbyCreated_t> _lobbyCreatedCallback;
        private Steamworks.Callback<Steamworks.LobbyEnter_t> _lobbyEnterCallback;

        // Steam API初始化标志
        private bool _isSteamApiInitialized = false;

        /// <summary>
        /// 初始化Steam房间系统
        /// </summary>
        /// <param name="networkManager">NetworkManager实例</param>
        public void Initialize(NetworkManager networkManager)
        {
            _networkManager = networkManager;

            // 获取SteamTransport组件
            _steamTransport = _networkManager.GetComponent<SteamTransport>();
            if (_steamTransport == null)
            {
                Debug.LogError("SteamTransport component not found on NetworkManager! Please add it.");
            }

            // 注册Steam API回调
            RegisterSteamCallbacks();
        }

        /// <summary>
        /// 注册Steam API回调
        /// </summary>
        private void RegisterSteamCallbacks()
        {
            _lobbyMatchListCallback = Steamworks.Callback<Steamworks.LobbyMatchList_t>.Create(OnLobbyMatchListCallback);
            _lobbyCreatedCallback = Steamworks.Callback<Steamworks.LobbyCreated_t>.Create(OnLobbyCreatedCallback);
            _lobbyEnterCallback = Steamworks.Callback<Steamworks.LobbyEnter_t>.Create(OnLobbyEnterCallback);
        }

        /// <summary>
        /// 连接到Steam
        /// </summary>
        public void ConnectToSteam()
        {
            bool success = false;

            try
            {
                // 检查Steam客户端是否正在运行
                if (!Steamworks.SteamAPI.IsSteamRunning())
                {
                    Debug.LogError("Steam client is not running!");
                    success = false;
                }
                else
                {
                    // 初始化Steam API
                    success = Steamworks.SteamAPI.Init();
                    _isSteamApiInitialized = success;

                    if (success)
                    {
                        Debug.Log("Steam API initialized successfully!");
                    }
                    else
                    {
                        Debug.LogError("Failed to initialize Steam API!");
                        Debug.LogError("Make sure Steam client is running and you're logged in!");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception while initializing Steam API: " + e.Message);
                success = false;
            }

            onSteamConnected?.Invoke(success);
        }

        /// <summary>
        /// 创建游戏房间
        /// </summary>
        /// <param name="roomName">房间名称</param>
        /// <param name="maxPlayers">最大玩家数</param>
        public void CreateRoom(string roomName, int maxPlayers = 10)
        {
            // 保存房间名称，用于回调处理
            PlayerPrefs.SetString("CreateRoomName", roomName);
            PlayerPrefs.SetInt("CreateRoomMaxPlayers", maxPlayers);

            // 创建Steam Lobby
            Steamworks.SteamMatchmaking.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypePublic, maxPlayers);
        }

        /// <summary>
        /// 获取可用房间列表
        /// </summary>
        public void GetRoomList()
        {
            _roomList.Clear();

            // 请求Steam Lobby列表
            // 只获取我们游戏的房间
            Steamworks.SteamMatchmaking.AddRequestLobbyListStringFilter("game", "ProjectZombie", Steamworks.ELobbyComparison.k_ELobbyComparisonEqual);
            Steamworks.SteamMatchmaking.AddRequestLobbyListResultCountFilter(20);
            Steamworks.SteamMatchmaking.AddRequestLobbyListDistanceFilter(Steamworks.ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            Steamworks.SteamMatchmaking.RequestLobbyList();
        }

        /// <summary>
        /// 加入指定房间
        /// </summary>
        /// <param name="roomInfo">房间信息</param>
        public void JoinRoom(SteamRoomInfo roomInfo)
        {
            if (string.IsNullOrEmpty(roomInfo.RoomId))
            {
                onRoomJoined?.Invoke(false);
                return;
            }

            JoinRoom(roomInfo.RoomId);
        }

        /// <summary>
        /// 通过房间ID加入房间
        /// </summary>
        /// <param name="roomId">房间ID</param>
        public void JoinRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId))
            {
                onRoomJoined?.Invoke(false);
                return;
            }

            // 转换房间ID为ulong
            if (ulong.TryParse(roomId, out ulong lobbyId))
            {
                // 加入Steam Lobby
                Steamworks.SteamMatchmaking.JoinLobby(new Steamworks.CSteamID(lobbyId));
            }
            else
            {
                onRoomJoined?.Invoke(false);
            }
        }

        /// <summary>
        /// Lobby匹配列表回调
        /// </summary>
        private void OnLobbyMatchListCallback(Steamworks.LobbyMatchList_t callback)
        {
            _roomList.Clear();
            Debug.Log("roomList");
            // 处理获取到的Lobby列表
            for (int i = 0; i < callback.m_nLobbiesMatching; i++)
            {
                // 获取Lobby ID
                Steamworks.CSteamID lobbyId = Steamworks.SteamMatchmaking.GetLobbyByIndex(i);
                if (lobbyId == Steamworks.CSteamID.Nil)
                    continue;

                // 创建房间信息
                var roomInfo = new SteamRoomInfo();
                roomInfo.RoomId = lobbyId.m_SteamID.ToString();
                roomInfo.RoomName = Steamworks.SteamMatchmaking.GetLobbyData(lobbyId, "name");
                roomInfo.PlayerCount = Steamworks.SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                roomInfo.MaxPlayers = Steamworks.SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

                // 获取房主名称
                Steamworks.CSteamID hostId = Steamworks.SteamMatchmaking.GetLobbyOwner(lobbyId);
                roomInfo.HostName = Steamworks.SteamFriends.GetFriendPersonaName(hostId);

                _roomList.Add(roomInfo);
            }

            // 通知房间列表更新
            onRoomListUpdated?.Invoke(_roomList);
        }

        /// <summary>
        /// Lobby创建回调
        /// </summary>
        private void OnLobbyCreatedCallback(Steamworks.LobbyCreated_t callback)
        {
            bool success = callback.m_eResult == Steamworks.EResult.k_EResultOK;
            Debug.Log("roomCreate");

            if (success)
            {
                Debug.Log("roomCreate success");

                // 获取创建房间时保存的信息
                string roomName = PlayerPrefs.GetString("CreateRoomName");
                int maxPlayers = PlayerPrefs.GetInt("CreateRoomMaxPlayers");

                Steamworks.CSteamID lobbyId = new Steamworks.CSteamID(callback.m_ulSteamIDLobby);

                // 设置房间名称
                Steamworks.SteamMatchmaking.SetLobbyData(lobbyId, "name", roomName);

                // 设置游戏标识，用于过滤房间列表
                Steamworks.SteamMatchmaking.SetLobbyData(lobbyId, "game", "ProjectZombie");


                // 先启动服务器
                _networkManager.StartServer();
                // 再启动客户端
                _networkManager.StartClient();
            }

            // 通知房间创建结果
            onRoomCreated?.Invoke(success);
        }

        /// <summary>
        /// Lobby进入回调
        /// </summary>
        private void OnLobbyEnterCallback(Steamworks.LobbyEnter_t callback)
        {
            bool success = true;
            Debug.Log("roomJoin success");

            Steamworks.CSteamID lobbyId = new Steamworks.CSteamID(callback.m_ulSteamIDLobby);

            // 获取房主Steam ID
            Steamworks.CSteamID hostId = Steamworks.SteamMatchmaking.GetLobbyOwner(lobbyId);


            // P2P模式，使用房主Steam ID连接
            _steamTransport.peerToPeer = true;
            _steamTransport.address = hostId.m_SteamID.ToString();
            Debug.Log($"Configured SteamTransport: P2P={_steamTransport.peerToPeer}, Address={_steamTransport.address}");


            _networkManager.StartClient();

            // 通知房间加入结果
            onRoomJoined?.Invoke(success);
        }

        /// <summary>
        /// 退出Steam
        /// </summary>
        public void DisconnectFromSteam()
        {
            Steamworks.SteamAPI.Shutdown();
        }

        private void Update()
        {
            // 只有在Steam API初始化后才更新Steam API
            if (_isSteamApiInitialized)
            {
                Steamworks.SteamAPI.RunCallbacks();
            }
        }

        private void OnApplicationQuit()
        {
            DisconnectFromSteam();
        }
    }
}