using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ButtonAttribute : PropertyAttribute
{
    public readonly string Name;
    public readonly float Height;
    public readonly bool EnabledInPlayMode;
    public readonly bool EnabledInEditMode;

    public ButtonAttribute(string name = null, float height = 20f, bool enabledInPlayMode = true, bool enabledInEditMode = true)
    {
        Name = name;
        Height = height;
        EnabledInPlayMode = enabledInPlayMode;
        EnabledInEditMode = enabledInEditMode;
    }
}