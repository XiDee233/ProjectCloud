using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PurrNet.Transports;

namespace Local
{
    public class LocalRoomManager : MonoBehaviour
    {
        [Serializable]
        public class LocalRoomInfo
        {
            public string RoomId;
            public string RoomName;
            public int PlayerCount;
            public int MaxPlayers;
            public string HostName;
            public string HostIp;
        }

        [Serializable]
        private class BroadcastMessage
        {
            public string Type;
            public LocalRoomInfo RoomInfo;
        }

        public event Action<bool> onLocalConnected;
        public event Action<List<LocalRoomInfo>> onRoomListUpdated;
        public event Action<bool> onRoomCreated;
        public event Action<bool> onRoomJoined;

        private NetworkManager _networkManager;
        private UDPTransport _udpTransport;

        [SerializeField]
        private List<LocalRoomInfo> _roomList = new List<LocalRoomInfo>();

        private int _serverPort = 5000;
        private LocalRoomInfo _currentRoom;

        private UdpClient _udpClient;
        private const int DISCOVERY_PORT = 5001;
        private const float BROADCAST_INTERVAL = 2f;
        private float _lastBroadcastTime;

        public void Initialize(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            _udpTransport = _networkManager.GetComponent<UDPTransport>();
            
            try
            {
                // 初始化 UDP 客户端，用于广播和监听
                _udpClient = new UdpClient();
                // 关键：允许地址复用，这样同一台电脑双开时，第二个实例也能绑定到同个端口
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DISCOVERY_PORT));
                _udpClient.EnableBroadcast = true;
                
                Debug.Log($"Local Room Discovery initialized on port {DISCOVERY_PORT}");
                onLocalConnected?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize UDP: {e.Message}");
                onLocalConnected?.Invoke(false);
            }
        }

        private void Update()
        {
            if (_udpClient == null) return;

            // 1. 检查是否有数据包到达（非阻塞）
            while (_udpClient.Available > 0)
            {
                try
                {
                    IPEndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient.Receive(ref senderEP);
                    string json = Encoding.UTF8.GetString(data);
                    BroadcastMessage msg = JsonUtility.FromJson<BroadcastMessage>(json);
                    
                    if (msg != null)
                    {
                        HandleMessage(msg, senderEP);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error receiving UDP message: {e.Message}");
                }
            }

            // 2. 如果是主机，定期广播房间信息
            if (_currentRoom != null && Time.time - _lastBroadcastTime > BROADCAST_INTERVAL)
            {
                _lastBroadcastTime = Time.time;
                BroadcastRoomInfo();
            }
        }

        private void HandleMessage(BroadcastMessage message, IPEndPoint senderEP)
        {
            // 收到“发现”请求，如果是主机就回应
            if (message.Type == "DISCOVER")
            {
                if (_currentRoom != null)
                {
                    BroadcastRoomInfo(); // 简单起见，收到请求直接广播一次
                }
            }
            // 收到“房间信息”，更新列表
            else if (message.Type == "ROOM_INFO" && message.RoomInfo != null)
            {
                // 过滤掉自己发出的消息（通过 RoomId 识别）
                if (_currentRoom != null && message.RoomInfo.RoomId == _currentRoom.RoomId)
                    return;

                // 自动纠正 IP：直接使用数据包来源的 IP
                message.RoomInfo.HostIp = senderEP.Address.ToString();
                
                AddOrUpdateRoom(message.RoomInfo);
            }
        }

        private void BroadcastRoomInfo()
        {
            if (_currentRoom == null) return;

            SendMessage(new BroadcastMessage 
            { 
                Type = "ROOM_INFO", 
                RoomInfo = _currentRoom 
            });
        }

        public void GetRoomList()
        {
            _roomList.Clear();
            // 向全网大喊一声：“谁在开房间？”
            SendMessage(new BroadcastMessage { Type = "DISCOVER" });
            
            // 稍后通知 UI 更新
            Invoke(nameof(NotifyRoomListUpdated), 0.5f);
        }

        private void SendMessage(BroadcastMessage message)
        {
            try
            {
                string json = JsonUtility.ToJson(message);
                byte[] data = Encoding.UTF8.GetBytes(json);
                // 广播给局域网所有人
                _udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT));
            }
            catch (Exception e)
            {
                Debug.LogError($"UDP Send Error: {e.Message}");
            }
        }

        private void AddOrUpdateRoom(LocalRoomInfo info)
        {
            int index = _roomList.FindIndex(r => r.RoomId == info.RoomId);
            if (index >= 0)
                _roomList[index] = info;
            else
                _roomList.Add(info);

            onRoomListUpdated?.Invoke(_roomList);
        }

        private void NotifyRoomListUpdated()
        {
            onRoomListUpdated?.Invoke(_roomList);
        }

        public void CreateRoom(string roomName, int maxPlayers = 10)
        {
            _currentRoom = new LocalRoomInfo
            {
                RoomId = Guid.NewGuid().ToString(),
                RoomName = roomName,
                PlayerCount = 1,
                MaxPlayers = maxPlayers,
                HostName = Environment.MachineName,
                // 这里暂时填空，接收方会根据 senderEP 自动识别真实 IP
                HostIp = "" 
            };

            _networkManager.StartServer();
            _networkManager.StartClient();
            onRoomCreated?.Invoke(true);
            
            Debug.Log($"Room '{roomName}' created. Broadcasting...");
        }

        public void JoinRoom(string hostIp)
        {
            if (string.IsNullOrEmpty(hostIp)) return;

            if (_udpTransport != null)
            {
                _udpTransport.address = hostIp;
                _udpTransport.serverPort = (ushort)_serverPort;
            }

            _networkManager.StartClient();
            onRoomJoined?.Invoke(true);
        }

        public void Disconnect()
        {
            _currentRoom = null;
            _networkManager.StopClient();
            _networkManager.StopServer();
        }

        private void OnDestroy()
        {
            if (_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;
            }
        }
    }
}
