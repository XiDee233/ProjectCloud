using System.Collections.Generic;
using UnityEngine;

namespace SceneManagement.Quadtree
{
    public class SceneQuadtree
    {
        private QuadtreeNode _root;
        private List<SceneData> _allScenes;
        private Bounds _worldBounds;

        public SceneQuadtree(Bounds worldBounds)
        {
            _worldBounds = worldBounds;
            _root = new QuadtreeNode(worldBounds);
            _allScenes = new List<SceneData>();
        }

        public void AddScene(SceneData scene)
        {
            if (!_allScenes.Contains(scene))
            {
                _allScenes.Add(scene);
                _root.Insert(scene);
            }
        }

        public void AddScenes(List<SceneData> scenes)
        {
            foreach (var scene in scenes)
            {
                AddScene(scene);
            }
        }

        public void RemoveScene(SceneData scene)
        {
            if (_allScenes.Contains(scene))
            {
                _allScenes.Remove(scene);
                Rebuild();
            }
        }

        public void Clear()
        {
            _allScenes.Clear();
            _root = new QuadtreeNode(_worldBounds);
        }

        public List<SceneData> QueryScenes(Vector3 playerPosition, float viewDistance)
        {
            Bounds queryBounds = new Bounds(playerPosition, new Vector3(viewDistance * 2, viewDistance * 2, viewDistance * 2));
            return _root.Query(queryBounds);
        }

        public List<SceneData> QueryScenes(Bounds queryBounds)
        {
            return _root.Query(queryBounds);
        }

        private void Rebuild()
        {
            _root = new QuadtreeNode(_worldBounds);
            foreach (var scene in _allScenes)
            {
                _root.Insert(scene);
            }
        }

        public void UpdateWorldBounds(Bounds newBounds)
        {
            _worldBounds = newBounds;
            Rebuild();
        }

        public List<SceneData> GetAllScenes()
        {
            return new List<SceneData>(_allScenes);
        }

        public int GetSceneCount()
        {
            return _allScenes.Count;
        }
    }
}