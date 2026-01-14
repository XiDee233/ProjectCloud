using UnityEngine;

namespace SceneManagement.Quadtree
{
    /// <summary>
    /// 场景数据类，用于存储场景的基本信息
    /// 每个场景都有名称、构建索引、位置和大小等信息
    /// </summary>
    [System.Serializable]
    public class SceneData
    {
        // 场景名称（用于加载场景）
        [SerializeField]
        private string _sceneName;
        
        // 场景的构建索引（Unity Build Settings 中的索引）
        [SerializeField]
        private int _buildIndex;
        
        // 场景在世界空间中的位置
        [SerializeField]
        private Vector3 _position;
        
        // 场景的大小（长宽高）
        [SerializeField]
        private Vector3 _size;
        
        // 场景的边界框（用于碰撞检测和空间查询）
        [SerializeField]
        private Bounds _bounds;

        // 公共属性，提供对私有字段的只读访问
        public string SceneName => _sceneName;
        public int BuildIndex => _buildIndex;
        public Vector3 Position => _position;
        public Vector3 Size => _size;
        public Bounds Bounds => _bounds;

        /// <summary>
        /// 构造函数，创建一个新的场景数据对象
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="buildIndex">场景的构建索引</param>
        /// <param name="position">场景在世界空间中的位置</param>
        /// <param name="size">场景的大小</param>
        public SceneData(string sceneName, int buildIndex, Vector3 position, Vector3 size)
        {
            _sceneName = sceneName;
            _buildIndex = buildIndex;
            _position = position;
            _size = size;
            // 根据位置和大小创建边界框
            _bounds = new Bounds(position, size);
        }

        /// <summary>
        /// 更新场景的边界框
        /// 当场景的位置或大小发生变化时调用此方法
        /// </summary>
        public void UpdateBounds()
        {
            // 根据当前位置和大小重新创建边界框
            _bounds = new Bounds(_position, _size);
        }
    }
}