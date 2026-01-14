using System.Collections.Generic;
using UnityEngine;

namespace SceneManagement.Quadtree
{
    /// <summary>
    /// 四叉树节点类，用于空间划分和场景数据管理
    /// 四叉树是一种树形数据结构，每个节点最多有4个子节点，常用于2D空间索引
    /// </summary>
    public class QuadtreeNode
    {
        // 节点的边界范围
        private Bounds _bounds;
        // 存储在该节点中的场景数据列表
        private List<SceneData> _scenes;
        // 子节点数组，四叉树每个节点最多有4个子节点
        private QuadtreeNode[] _children;
        // 标记该节点是否已经进行了细分
        private bool _isSubdivided;
        // 每个节点最多存储的场景数量，超过此数量将触发细分
        private int _maxScenesPerNode = 4;
        // 允许细分的最小节点尺寸，当节点尺寸小于等于此值时不再细分
        private float _minNodeSize = 1000f;

        // 公共属性，提供对私有字段的只读访问
        public Bounds Bounds => _bounds;
        public bool IsSubdivided => _isSubdivided;
        public QuadtreeNode[] Children => _children;
        public List<SceneData> Scenes => _scenes;

        /// <summary>
        /// 构造函数，创建一个新的四叉树节点
        /// </summary>
        /// <param name="bounds">节点的边界范围</param>
        /// <param name="maxScenesPerNode">每个节点最多存储的场景数</param>
        /// <param name="minNodeSize">允许细分的最小节点尺寸，节点尺寸小于等于此值时不再细分</param>
        public QuadtreeNode(Bounds bounds, int maxScenesPerNode = 4, float minNodeSize = 1000f)
        {
            _bounds = bounds;
            _maxScenesPerNode = maxScenesPerNode;
            _minNodeSize = minNodeSize;
            _scenes = new List<SceneData>();
            _children = new QuadtreeNode[4];
            _isSubdivided = false;
        }

        /// <summary>
        /// 向四叉树中插入场景数据
        /// 如果场景不在节点边界内，则不插入
        /// 如果节点已细分，则递归插入到子节点
        /// 如果节点未细分且场景数量超过阈值，则细分节点并重新分配场景
        /// </summary>
        /// <param name="scene">要插入的场景数据</param>
        public void Insert(SceneData scene)
        {
            // 如果场景不在当前节点的边界内，直接返回
            if (!_bounds.Intersects(scene.Bounds))
            {
                return;
            }

            // 如果节点已经细分，将场景递归插入到所有相交的子节点中
            if (_isSubdivided)
            {
                for (int i = 0; i < 4; i++)
                {
                    _children[i].Insert(scene);
                }
                return;
            }

            // 节点未细分，将场景添加到当前节点的场景列表
            _scenes.Add(scene);

            // 检查是否需要细分：场景数量超过阈值且节点尺寸大于允许细分的最小尺寸
            // 注意：_minNodeSize 是"允许细分的最小节点尺寸"
            // 例如：_minNodeSize = 1000 表示只有节点尺寸大于1000时才能继续细分
            // 当节点尺寸 <= 1000 时，即使场景数量超过阈值也不会再细分，避免无限递归
            if (_scenes.Count > _maxScenesPerNode && _bounds.size.x > _minNodeSize)
            {
                Subdivide();
                
                // 将当前节点的所有场景重新分配到子节点中
                for (int i = _scenes.Count - 1; i >= 0; i--)
                {
                    var sceneToInsert = _scenes[i];
                    for (int j = 0; j < 4; j++)
                    {
                        _children[j].Insert(sceneToInsert);
                    }
                    _scenes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 将当前节点细分为4个子节点
        /// 每个子节点占据原节点的1/4空间
        /// </summary>
        private void Subdivide()
        {
            // 计算子节点的大小（原节点大小的一半）
            Vector3 halfSize = _bounds.size / 2f;
            // 获取原节点的中心点
            Vector3 center = _bounds.center;

            // 创建4个子节点，每个子节点占据原节点的1/4空间
            // 子节点0：右上象限（X+，Z+）
            _children[0] = new QuadtreeNode(
                new Bounds(center + new Vector3(halfSize.x / 2, 0, halfSize.z / 2), halfSize),
                _maxScenesPerNode, _minNodeSize);
            
            // 子节点1：左上象限（X-，Z+）
            _children[1] = new QuadtreeNode(
                new Bounds(center + new Vector3(-halfSize.x / 2, 0, halfSize.z / 2), halfSize),
                _maxScenesPerNode, _minNodeSize);
            
            // 子节点2：左下象限（X-，Z-）
            _children[2] = new QuadtreeNode(
                new Bounds(center + new Vector3(-halfSize.x / 2, 0, -halfSize.z / 2), halfSize),
                _maxScenesPerNode, _minNodeSize);
            
            // 子节点3：右下象限（X+，Z-）
            _children[3] = new QuadtreeNode(
                new Bounds(center + new Vector3(halfSize.x / 2, 0, -halfSize.z / 2), halfSize),
                _maxScenesPerNode, _minNodeSize);

            // 标记节点已细分
            _isSubdivided = true;
        }

        /// <summary>
        /// 查询与给定边界相交的所有场景
        /// 如果节点已细分，则递归查询所有相交的子节点
        /// 否则，查询当前节点中的场景列表
        /// </summary>
        /// <param name="queryBounds">查询的边界范围</param>
        /// <param name="results">结果列表，用于存储找到的场景</param>
        /// <returns>与查询边界相交的场景列表</returns>
        public List<SceneData> Query(Bounds queryBounds, List<SceneData> results = null)
        {
            // 如果结果列表为空，创建一个新的列表
            if (results == null)
            {
                results = new List<SceneData>();
            }

            // 如果查询边界与当前节点边界不相交，直接返回
            if (!_bounds.Intersects(queryBounds))
            {
                return results;
            }

            // 如果节点已细分，递归查询所有子节点
            if (_isSubdivided)
            {
                for (int i = 0; i < 4; i++)
                {
                    _children[i].Query(queryBounds, results);
                }
            }
            else
            {
                // 节点未细分，检查当前节点中的每个场景是否与查询边界相交
                for (int i = 0; i < _scenes.Count; i++)
                {
                    if (queryBounds.Intersects(_scenes[i].Bounds))
                    {
                        results.Add(_scenes[i]);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 清空节点中的所有场景数据
        /// 如果节点已细分，则递归清空所有子节点
        /// </summary>
        public void Clear()
        {
            // 清空当前节点的场景列表
            _scenes.Clear();
            
            // 如果节点已细分，递归清空所有子节点
            if (_isSubdivided)
            {
                for (int i = 0; i < 4; i++)
                {
                    _children[i].Clear();
                    _children[i] = null;
                }
                // 标记节点未细分
                _isSubdivided = false;
            }
        }
    }
}