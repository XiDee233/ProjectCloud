using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet.Modules;
using SceneManagement.Managers;

namespace SceneManagement.Quadtree
{
    public class SceneQuadtreeManager : MonoBehaviour
    {
        [Header("Quadtree Settings")]
        [SerializeField]
        private Bounds _worldBounds = new Bounds(Vector3.zero, new Vector3(10000f, 10000f, 10000f));
        
        [Header("Scene Settings")]
        [SerializeField]
        private List<SceneData> _scenes = new List<SceneData>();
        
        [Header("Persistent Scene")]
        [SerializeField]
        private string _persistentSceneName = "Persistent";
        
        private SceneQuadtree _quadtree;
        private HashSet<string> _loadedScenes = new HashSet<string>();
        private ScenesModule _scenesModule;
        
        // 将单一引用改为列表，支持多个PlayerSceneTracker
        private List<PlayerSceneTracker> _playerTrackers = new List<PlayerSceneTracker>();



        public void Initialize()
        {
            _quadtree = new SceneQuadtree(_worldBounds);
            _quadtree.AddScenes(_scenes);
            _loadedScenes = new HashSet<string>();
            
            // Add persistent scene to loaded scenes
            _loadedScenes.Add(_persistentSceneName);
        }
        
        // 添加注册方法，用于PlayerSceneTracker注册到管理器
        public void RegisterTracker(PlayerSceneTracker tracker)
        {
            _playerTrackers.Add(tracker);
            tracker.onPlayerMoved += OnPlayerMoved;
        }
        
        // 添加注销方法，用于PlayerSceneTracker从管理器注销
        public void UnregisterTracker(PlayerSceneTracker tracker)
        {
            if (_playerTrackers.Remove(tracker))
            {
                tracker.onPlayerMoved -= OnPlayerMoved;
            }
        }

        private void OnPlayerMoved(Vector3 playerPosition, float viewDistance)
        {
            UpdateScenes(playerPosition, viewDistance);
        }

        private void UpdateScenes(Vector3 playerPosition, float viewDistance)
        {
            // Get all scenes within view distance
            List<SceneData> scenesToLoad = _quadtree.QueryScenes(playerPosition, viewDistance);
            
            // Create a set of scene names that should be loaded
            HashSet<string> shouldBeLoaded = new HashSet<string>();
            shouldBeLoaded.Add(_persistentSceneName);
            
            foreach (var sceneData in scenesToLoad)
            {
                shouldBeLoaded.Add(sceneData.SceneName);
            }
            
            // Unload scenes that are no longer in view
            List<string> scenesToUnload = new List<string>();
            foreach (var loadedScene in _loadedScenes)
            {
                if (!shouldBeLoaded.Contains(loadedScene) && loadedScene != _persistentSceneName)
                {
                    scenesToUnload.Add(loadedScene);
                }
            }
            
            // Load new scenes that are in view
            List<string> scenesToAdd = new List<string>();
            foreach (var sceneName in shouldBeLoaded)
            {
                if (!_loadedScenes.Contains(sceneName))
                {
                    scenesToAdd.Add(sceneName);
                }
            }
            
            // Execute scene operations
            UnloadScenes(scenesToUnload);
            LoadScenes(scenesToAdd);
        }

        private void LoadScenes(List<string> scenesToLoad)
        {
            foreach (var sceneName in scenesToLoad)
            {
                LoadScene(sceneName);
            }
        }

        private void UnloadScenes(List<string> scenesToUnload)
        {
            foreach (var sceneName in scenesToUnload)
            {
                UnloadScene(sceneName);
            }
        }

        private void LoadScene(string sceneName)
        {
            if (_loadedScenes.Contains(sceneName))
            {
                return;
            }
            
            Debug.Log($"Loading scene: {sceneName}");
            
            if (_scenesModule != null)
            {
                // Use PurrNet ScenesModule for networked scene loading
                _scenesModule.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            else
            {
                // Fallback to direct SceneManager for testing
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            
            _loadedScenes.Add(sceneName);
        }

        private void UnloadScene(string sceneName)
        {
            if (!_loadedScenes.Contains(sceneName) || sceneName == _persistentSceneName)
            {
                return;
            }
            
            Debug.Log($"Unloading scene: {sceneName}");
            
            if (_scenesModule != null)
            {
                // Use PurrNet ScenesModule for networked scene unloading
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid())
                {
                    _scenesModule.UnloadSceneAsync(scene);
                }
            }
            else
            {
                // Fallback to direct SceneManager for testing
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid())
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
            
            _loadedScenes.Remove(sceneName);
        }

        // Setter for ScenesModule, to be called by GameManager
        public void SetScenesModule(ScenesModule scenesModule)
        {
            _scenesModule = scenesModule;
        }

        public void AddScene(SceneData scene)
        {
            _quadtree.AddScene(scene);
            _scenes.Add(scene);
        }

        public void RemoveScene(SceneData scene)
        {
            _quadtree.RemoveScene(scene);
            _scenes.Remove(scene);
        }

        public void ClearScenes()
        {
            _quadtree.Clear();
            _scenes.Clear();
        }

        public List<SceneData> GetAllScenes()
        {
            return _quadtree.GetAllScenes();
        }

        public List<SceneData> GetScenesInView(Vector3 position, float viewDistance)
        {
            return _quadtree.QueryScenes(position, viewDistance);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(_worldBounds.center, _worldBounds.size);
            
            if (_quadtree != null)
            {
                DrawQuadtreeGizmos(_quadtree);
            }
        }

        private void DrawQuadtreeGizmos(SceneQuadtree quadtree)
        {
            // This would recursively draw the quadtree nodes
            // For simplicity, we'll just draw the loaded scenes
            Gizmos.color = Color.green;
            foreach (var sceneName in _loadedScenes)
            {
                if (sceneName == _persistentSceneName)
                {
                    continue;
                }
                
                foreach (var sceneData in _scenes)
                {
                    if (sceneData.SceneName == sceneName)
                    {
                        Gizmos.DrawWireCube(sceneData.Bounds.center, sceneData.Bounds.size);
                        break;
                    }
                }
            }
        }
    }
}