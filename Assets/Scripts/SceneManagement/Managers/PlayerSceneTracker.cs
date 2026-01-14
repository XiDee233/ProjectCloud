using UnityEngine;
using PurrNet;
using SceneManagement.Quadtree;

namespace SceneManagement.Managers
{
    /// <summary>
    /// 玩家场景追踪器，用于追踪玩家位置并触发场景加载/卸载事件
    /// 建议直接挂载在玩家游戏对象上使用
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerSceneTracker : MonoBehaviour
    {
        [Header("更新设置")]
        [Tooltip("位置更新的时间间隔（秒）")]
        public float updateInterval = 2.0f;
        
        [Header("视距设置")]
        [Tooltip("玩家的场景加载视距")]
        public float viewDistance = 800.0f;
        
        [Header("移动阈值")]
        [Tooltip("触发位置更新的最小移动距离（视距的百分比）")]
        public float movementThreshold = 0.2f;
        
        // 玩家位置更新事件
        public System.Action<Vector3, float> onPlayerMoved;
        
        // 内部变量
        private Vector3 _lastPosition;
        private float _lastUpdateTime;
        private Transform _playerTransform;
        private NetworkIdentity _networkIdentity;
        
        private void Awake()
        {
            // 直接使用当前物体的transform作为玩家transform
            // 这样挂载在玩家身上时就会自动追踪玩家位置
            _playerTransform = transform;
            
            // 尝试获取NetworkIdentity组件，用于网络标识
            _networkIdentity = GetComponent<NetworkIdentity>();
            
            // 初始化位置和时间
            _lastPosition = _playerTransform.position;
            _lastUpdateTime = Time.time;
        }
        
        private void OnEnable()
        {
            // 自动注册到SceneQuadtreeManager
            SceneQuadtreeManager manager = GameManager.main.GetSceneQuadtreeManager();
            if (manager != null)
            {
                manager.RegisterTracker(this);
            }
        }
        
        private void OnDisable()
        {
            // 自动从SceneQuadtreeManager注销
            SceneQuadtreeManager manager = GameManager.main.GetSceneQuadtreeManager();
            if (manager != null)
            {
                manager.UnregisterTracker(this);
            }
        }
        
        private void Update()
        {
            // 只有本地玩家或服务器需要更新场景
            bool isLocalPlayer = _networkIdentity == null || _networkIdentity.isOwner;
            if (!isLocalPlayer && !NetworkManager.main.isServer)
            {
                return;
            }
            
            // 计算时间和距离
            float timeSinceLastUpdate = Time.time - _lastUpdateTime;
            float distanceMoved = Vector3.Distance(_playerTransform.position, _lastPosition);
            
            // 检查是否需要更新
            if (timeSinceLastUpdate >= updateInterval || distanceMoved >= viewDistance * movementThreshold)
            {
                // 更新位置和时间
                _lastPosition = _playerTransform.position;
                _lastUpdateTime = Time.time;
                
                // 触发位置更新事件
                onPlayerMoved?.Invoke(_playerTransform.position, viewDistance);
            }
        }
        
        /// <summary>
        /// 设置玩家transform（用于特殊情况）
        /// </summary>
        /// <param name="newTransform">新的玩家transform</param>
        public void SetPlayerTransform(Transform newTransform)
        {
            _playerTransform = newTransform;
            _lastPosition = _playerTransform.position;
        }
        
        /// <summary>
        /// 获取当前玩家位置
        /// </summary>
        /// <returns>玩家当前位置</returns>
        public Vector3 GetCurrentPosition()
        {
            return _playerTransform != null ? _playerTransform.position : Vector3.zero;
        }
        
        /// <summary>
        /// 检查是否是本地玩家
        /// </summary>
        /// <returns>是否是本地玩家</returns>
        public bool IsLocalPlayer()
        {
            return _networkIdentity == null || _networkIdentity.isOwner;
        }
    }
}