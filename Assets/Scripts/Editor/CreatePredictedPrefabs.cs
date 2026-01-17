using UnityEngine;
using UnityEditor;
using PurrNet.Prediction;

namespace ProjectZombie.Editor
{
    public static class CreatePredictedPrefabs
    {
        [MenuItem("Tools/PurrNet/Create PredictedPrefabs")]
        public static void CreateAsset()
        {
            var asset = ScriptableObject.CreateInstance<PredictedPrefabs>();
            
            string path = "Assets/PredictedPrefabs.asset";
            
            // Ensure the directory exists
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            
            Debug.Log($"Created PredictedPrefabs at {path}");
        }
    }
}