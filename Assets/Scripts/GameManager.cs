using UnityEngine;
using PurrNet;
using PurrNet.Modules;
using SceneManagement.Quadtree;

namespace SceneManagement
{
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        // 单例实例
        public static GameManager main { get; private set; }

        // 核心组件引用
        [SerializeField]
        private NetworkManager _networkManager;
        [SerializeField]
        private SceneQuadtreeManager _sceneQuadtreeManager;
        [SerializeField]
        private RoomManager _roomManager;
        
        // 游戏状态
        private bool _isGameStarted = false;
        
        private void Awake()
        {
            // 初始化单例
            if (main != null && main != this)
            {
                Destroy(gameObject);
                return;
            }
            main = this;
            
            // 初始化核心组件
            InitializeCoreComponents();
        }
        
        private void InitializeCoreComponents()
        {
            Debug.Log("GameManager initialization completed!");
            _roomManager.InitializeRoomSystems();
            _sceneQuadtreeManager.Initialize();
        }
        
        /// <summary>
        /// 启动游戏
        /// </summary>
        [Button]
        public void StartGame()
        {
            _isGameStarted = true;
            Debug.Log("Game started");
        }
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        [Button]
        public void ExitGame()
        {
            _isGameStarted = false;
            Debug.Log("Game exited");
        }
        
        // 提供公共方法获取核心组件
        public NetworkManager GetNetworkManager() => _networkManager;
        public SceneQuadtreeManager GetSceneQuadtreeManager() => _sceneQuadtreeManager;
        
        // 游戏状态访问器
        public bool IsGameStarted => _isGameStarted;
    }
}