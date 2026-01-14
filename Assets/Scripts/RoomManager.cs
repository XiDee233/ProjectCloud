using UnityEngine;
using PurrNet;
using Steam;
using Local;
using PurrNet.Steam;
using PurrNet.Transports;

namespace SceneManagement
{
    [DefaultExecutionOrder(-999)]
    public class RoomManager : MonoBehaviour
    {


        // 游戏模式枚举
        public enum GameMode
        {
            SinglePlayer,
            MultiPlayer
        }

        // 连接模式枚举
        public enum ConnectionMode
        {
            None,
            Steam,
            Local
        }

        // 核心组件引用
        [SerializeField]
        private NetworkManager _networkManager;
        [SerializeField]
        private SteamRoomManager _steamRoomSystem;
        [SerializeField]
        private LocalRoomManager _localRoomSystem;

        // 游戏状态
        private GameMode _gameMode = GameMode.SinglePlayer;
        private ConnectionMode _connectionMode = ConnectionMode.None;



        /// <summary>
        /// 初始化房间系统
        /// </summary>
        public void InitializeRoomSystems()
        {
            _networkManager = GameManager.main.GetNetworkManager();

            _steamRoomSystem.Initialize(_networkManager);

            _localRoomSystem.Initialize(_networkManager);

            Debug.Log("RoomManager initialization completed!");
        }

        /// <summary>
        /// 设置连接模式（Steam或本地）
        /// </summary>
        /// <param name="mode">连接模式</param>
        [Button]
        public void SetConnectionMode(ConnectionMode mode)
        {
            _connectionMode = mode;

            // 切换传输组件
            SwitchTransportComponent(mode);

            if (mode == ConnectionMode.Steam)
            {
                // Steam模式
                SubscribeToSteamEvents();
                // 连接到Steam
                _steamRoomSystem.ConnectToSteam();
            }
            else if (mode == ConnectionMode.Local)
            {
                // 本地模式
                // 本地模式不需要特殊连接
            }

            Debug.Log($"Connection mode set to: {mode}");
        }
        
        /// <summary>
        /// 切换传输组件
        /// </summary>
        /// <param name="mode">连接模式</param>
        private void SwitchTransportComponent(ConnectionMode mode)
        {
            if (_networkManager == null)
            {
                Debug.LogError("NetworkManager is null, cannot switch transport component");
                return;
            }
            
            var steamTransport = _networkManager.GetComponent<SteamTransport>();
            var udpTransport = _networkManager.GetComponent<UDPTransport>();
            
            if (steamTransport == null || udpTransport == null)
            {
                Debug.LogError("Transport components not found on NetworkManager");
                return;
            }
            
            switch (mode)
            {
                case ConnectionMode.Steam:
                    _networkManager.transport = steamTransport;
                    Debug.Log("Switched to SteamTransport");
                    break;
                case ConnectionMode.Local:
                    _networkManager.transport = udpTransport;
                    Debug.Log("Switched to UDPTransport");
                    break;
            }
        }

        /// <summary>
        /// 订阅SteamRoomSystem事件
        /// </summary>
        private void SubscribeToSteamEvents()
        {

            // 订阅Steam连接事件
            _steamRoomSystem.onSteamConnected += OnSteamConnected;
        }

        /// <summary>
        /// Steam连接事件处理
        /// </summary>
        /// <param name="success">连接是否成功</param>
        private void OnSteamConnected(bool success)
        {
            Debug.Log($"Steam connection: {success}");

            if (!success)
            {
                Debug.LogWarning("Steam connection failed. Please make sure Steam client is running and you're logged in.");
            }
        }

        /// <summary>
        /// 设置游戏模式（单人或多人）
        /// </summary>
        /// <param name="mode">游戏模式</param>
        [Button]
        public void SetGameMode(GameMode mode)
        {
            _gameMode = mode;
            Debug.Log($"Game mode set to: {mode}");
        }

        /// <summary>
        /// 创建游戏房间
        /// </summary>
        /// <param name="roomName">房间名称</param>
        /// <param name="maxPlayers">最大玩家数</param>
        [Button]
        public void CreateGameRoom(string roomName, int maxPlayers = 10)
        {
            // 确保设置了连接模式
            if (_connectionMode == ConnectionMode.None)
            {
                Debug.LogWarning("Connection mode not set. Please set connection mode first.");
                return;
            }

            if (_connectionMode == ConnectionMode.Local)
            {

                Debug.Log($"Creating local game room: {roomName}");
                _localRoomSystem.CreateRoom(roomName, maxPlayers);
            }
            else if (_connectionMode == ConnectionMode.Steam)
            {
                // Steam模式创建房间

                Debug.Log($"Creating Steam game room: {roomName}");
                _steamRoomSystem.CreateRoom(roomName, maxPlayers);
            }
        }

        /// <summary>
        /// 获取可用房间列表
        /// </summary>
        [Button]
        public void GetAvailableRooms()
        {
            // 确保设置了连接模式
            if (_connectionMode == ConnectionMode.None)
            {
                Debug.LogWarning("Connection mode not set. Please set connection mode first.");
                return;
            }

            if (_connectionMode == ConnectionMode.Local)
            {
                // 局域网模式获取房间列表
                if (_localRoomSystem != null)
                {
                    Debug.Log("Getting available local rooms");
                    _localRoomSystem.GetRoomList();
                }
            }
            else if (_connectionMode == ConnectionMode.Steam)
            {
                // Steam模式获取房间列表
                if (_steamRoomSystem != null)
                {
                    Debug.Log("Getting available Steam rooms");
                    _steamRoomSystem.GetRoomList();
                }
            }
        }

        /// <summary>
        /// 通过房间ID加入房间
        /// </summary>
        /// <param name="roomId">房间ID</param>
        [Button]
        public void JoinGameRoom(string roomId)
        {
            // 确保设置了连接模式
            if (_connectionMode == ConnectionMode.None)
            {
                Debug.LogWarning("Connection mode not set. Please set connection mode first.");
                return;
            }

            if (_connectionMode == ConnectionMode.Local)
            {
                // 局域网模式通过ip加入房间
                if (_localRoomSystem != null)
                {
                    Debug.Log($"Joining local game room with HostIP: {roomId}");
                    _localRoomSystem.JoinRoom(roomId);
                }
            }
            else if (_connectionMode == ConnectionMode.Steam)
            {
                // Steam模式通过ID加入房间
                if (_steamRoomSystem != null)
                {
                    Debug.Log($"Joining Steam game room with ID: {roomId}");
                    _steamRoomSystem.JoinRoom(roomId);
                }
            }
        }

        /// <summary>
        /// 退出游戏房间
        /// </summary>
        [Button]
        public void ExitGameRoom()
        {
            if (_connectionMode == ConnectionMode.Local)
            {

                _localRoomSystem.Disconnect();
            }
            else if (_connectionMode == ConnectionMode.Steam)
            {

                // 取消订阅事件
                _steamRoomSystem.onSteamConnected -= OnSteamConnected;
                // 断开Steam连接
                _steamRoomSystem.DisconnectFromSteam();
            }

            // 重置连接模式
            _connectionMode = ConnectionMode.None;
        }

        // 提供公共方法获取核心组件
        public NetworkManager GetNetworkManager() => _networkManager;
        public SteamRoomManager GetSteamRoomSystem() => _steamRoomSystem;
        public LocalRoomManager GetLocalRoomSystem() => _localRoomSystem;

        // 游戏状态访问器
        public GameMode CurrentGameMode => _gameMode;
        public ConnectionMode CurrentConnectionMode => _connectionMode;
    }
}