using System;
using System.Reflection;
using UnityEditor;

namespace sapra.InfiniteLands.Editor{
    public static class ClipboardReflectionHelper
    {
        public static void CallSetSerializedProperty(SerializedProperty src)
        {
            Type clipboardType = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.Clipboard");

            if (clipboardType == null)
            {
                UnityEngine.Debug.LogError("Clipboard type not found.");
                return;
            }

            MethodInfo method = clipboardType.GetMethod("SetSerializedProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                UnityEngine.Debug.LogError("SetSerializedProperty method not found.");
                return;
            }

            method.Invoke(null, new object[] { src });
        }

        public static void CallGetSerializedProperty(SerializedProperty dst)
        {
            Type clipboardType = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.Clipboard");

            if (clipboardType == null)
            {
                UnityEngine.Debug.LogError("Clipboard type not found.");
                return;
            }

            MethodInfo method = clipboardType.GetMethod("GetSerializedProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                UnityEngine.Debug.LogError("GetSerializedProperty method not found.");
                return;
            }

            method.Invoke(null, new object[] { dst });
        }

        public static bool CallHasSerializedProperty()
        {
            Type clipboardType = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.Clipboard");

            if (clipboardType == null)
            {
                UnityEngine.Debug.LogError("Clipboard type not found.");
                return false;
            }

            MethodInfo method = clipboardType.GetMethod("HasSerializedProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                UnityEngine.Debug.LogError("HasSerializedProperty method not found.");
                return false;
            }

            return (bool)method.Invoke(null, null);
        }
    }
}