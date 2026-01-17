#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
 
namespace MoreMountains.Tools
{
	[CustomPropertyDrawer(typeof(MMNavMeshAreaMaskAttribute))]
	public class MMNavMeshAreaMaskAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty serializedProperty, GUIContent label)
		{
#if UNITY_6000_OR_NEWER
			string[] navMeshAreaNames  = UnityEngine.AI.NavMesh.GetAreaNames();
#else
			string[] navMeshAreaNames = new string[] { "Walkable", "Not Walkable", "Jump" }; // Fallback for Unity 2022.3
#endif
			float positionWidth = position.width;
			int maskValue = serializedProperty.intValue;
			
			position.width = EditorGUIUtility.labelWidth;
			EditorGUI.PrefixLabel(position, label);
         
			position.x += EditorGUIUtility.labelWidth;
			position.width = positionWidth - EditorGUIUtility.labelWidth;
 
			EditorGUI.BeginChangeCheck();
			maskValue = EditorGUI.MaskField(position, maskValue, navMeshAreaNames);
			
			if (EditorGUI.EndChangeCheck())
			{
				serializedProperty.intValue = maskValue;
			}
		}
	}
}
#endif