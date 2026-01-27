using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // 禁用编辑
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;  // 重新启用，确保后续控件正常
    }
}