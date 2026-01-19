#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using System.IO;
using Player.Combat;

namespace Player.Combat.Editor
{
    public class CombatTimelineEditor : EditorWindow
    {
        [MenuItem("Window/Player/Combat Timeline Editor")]
        public static void ShowWindow()
        {
            GetWindow<CombatTimelineEditor>("Combat Editor");
        }

        private CombatTimelineData _currentData;
        private TimelineAsset _selectedAsset;

        private void OnGUI()
        {
            GUILayout.Label("战斗动作编辑器", EditorStyles.boldLabel);
            
            _currentData = (CombatTimelineData)EditorGUILayout.ObjectField("当前数据", _currentData, typeof(CombatTimelineData), false);
            _selectedAsset = (TimelineAsset)EditorGUILayout.ObjectField("Timeline 资源", _selectedAsset, typeof(TimelineAsset), false);

            if (GUILayout.Button("创建新战斗数据"))
            {
                CreateNewData();
            }

            if (_currentData != null && _selectedAsset != null)
            {
                if (GUILayout.Button("应用 Timeline 到数据"))
                {
                    _currentData.SetTimeline(_selectedAsset);
                    EditorUtility.SetDirty(_currentData);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"已将 {_selectedAsset.name} 应用到 {_currentData.name}");
                }
            }
        }

        private void CreateNewData()
        {
            string path = EditorUtility.SaveFilePanelInProject("保存战斗数据", "NewCombatData", "asset", "请输入文件名");
            if (string.IsNullOrEmpty(path)) return;

            var data = CreateInstance<CombatTimelineData>();
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            _currentData = data;
        }
    }
}
#endif
