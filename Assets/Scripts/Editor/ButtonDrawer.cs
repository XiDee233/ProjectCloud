using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class ButtonDrawer : Editor
{
    // 用于存储每个方法的参数值
    private Dictionary<string, object[]> _methodParameters = new Dictionary<string, object[]>();
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        serializedObject.Update();
        
        Type type = target.GetType();
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        foreach (MethodInfo method in methods)
        {
            ButtonAttribute buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
            if (buttonAttribute != null)
            {
                ParameterInfo[] parameters = method.GetParameters();
                
                bool isPlaying = Application.isPlaying;
                bool isEnabled = (isPlaying && buttonAttribute.EnabledInPlayMode) || (!isPlaying && buttonAttribute.EnabledInEditMode);
                
                EditorGUI.BeginDisabledGroup(!isEnabled);
                
                string buttonName = string.IsNullOrEmpty(buttonAttribute.Name) ? ObjectNames.NicifyVariableName(method.Name) : buttonAttribute.Name;
                
                // 生成参数输入控件
                object[] parameterValues = GetOrCreateParameterValues(method, parameters);
                bool hasValidParameters = DrawParameters(method, parameters, ref parameterValues);
                
                // 生成按钮
                if (GUILayout.Button(buttonName, GUILayout.Height(buttonAttribute.Height)) && hasValidParameters)
                {
                    foreach (UnityEngine.Object obj in targets)
                    {
                        try
                        {
                            method.Invoke(obj, parameterValues);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error calling method {method.Name} on {obj.name}: {e.InnerException?.Message ?? e.Message}");
                        }
                    }
                }
                
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    // 获取或创建参数值数组
    private object[] GetOrCreateParameterValues(MethodInfo method, ParameterInfo[] parameters)
    {
        string methodKey = $"{method.DeclaringType.FullName}.{method.Name}";
        
        if (!_methodParameters.TryGetValue(methodKey, out object[] values) || values.Length != parameters.Length)
        {
            values = new object[parameters.Length];
            
            // 初始化默认值
            for (int i = 0; i < parameters.Length; i++)
            {
                values[i] = GetDefaultValue(parameters[i].ParameterType);
            }
            
            _methodParameters[methodKey] = values;
        }
        
        return values;
    }
    
    // 获取类型的默认值
    private object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
    
    // 绘制参数输入控件
    private bool DrawParameters(MethodInfo method, ParameterInfo[] parameters, ref object[] parameterValues)
    {
        if (parameters.Length == 0)
        {
            return true;
        }
        
        bool allValid = true;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo param = parameters[i];
            object currentValue = parameterValues[i];
            
            EditorGUILayout.BeginHorizontal();
            
            // 参数名称
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(param.Name), GUILayout.Width(100f));
            
            // 根据参数类型绘制不同的控件
            object newValue = DrawParameterControl(param.ParameterType, currentValue);
            
            if (newValue != null || param.ParameterType.IsValueType)
            {
                parameterValues[i] = newValue;
            }
            else
            {
                allValid = false;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
        
        return allValid;
    }
    
    // 根据类型绘制参数控件
    private object DrawParameterControl(Type type, object currentValue)
    {
        // 基本类型
        if (type == typeof(int))
        {
            return EditorGUILayout.IntField((int)(currentValue ?? 0));
        }
        else if (type == typeof(float))
        {
            return EditorGUILayout.FloatField((float)(currentValue ?? 0f));
        }
        else if (type == typeof(bool))
        {
            return EditorGUILayout.Toggle((bool)(currentValue ?? false));
        }
        else if (type == typeof(string))
        {
            return EditorGUILayout.TextField((string)currentValue ?? "");
        }
        else if (type == typeof(Vector2))
        {
            return EditorGUILayout.Vector2Field(GUIContent.none, (Vector2)(currentValue ?? Vector2.zero));
        }
        else if (type == typeof(Vector3))
        {
            return EditorGUILayout.Vector3Field(GUIContent.none, (Vector3)(currentValue ?? Vector3.zero));
        }
        else if (type == typeof(Vector4))
        {
            return EditorGUILayout.Vector4Field(GUIContent.none, (Vector4)(currentValue ?? Vector4.zero));
        }
        else if (type == typeof(Color))
        {
            return EditorGUILayout.ColorField((Color)(currentValue ?? Color.white));
        }
        else if (type == typeof(Quaternion))
        {
            Vector3 euler = (currentValue != null) ? ((Quaternion)currentValue).eulerAngles : Vector3.zero;
            euler = EditorGUILayout.Vector3Field(GUIContent.none, euler);
            return Quaternion.Euler(euler);
        }
        // UnityEngine.Object类型
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return EditorGUILayout.ObjectField((UnityEngine.Object)currentValue, type, true);
        }
        // 枚举类型
        else if (type.IsEnum)
        {
            return EditorGUILayout.EnumPopup((Enum)(currentValue ?? Activator.CreateInstance(type)));
        }
        
        // 不支持的类型
        EditorGUILayout.HelpBox($"不支持的参数类型: {type.Name}", MessageType.Warning);
        return currentValue;
    }
}