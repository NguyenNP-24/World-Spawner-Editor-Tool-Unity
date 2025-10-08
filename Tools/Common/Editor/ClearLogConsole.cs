using System.Reflection;
using UnityEditor;
using System;

namespace Tools
{
    /// <summary>
    /// Utility class to clear the Unity Console via reflection
    /// </summary>
    public static class ClearLogConsole
    {
        public static void Clear()
        {
#if UNITY_EDITOR
            // Use reflection to access the internal LogEntries class and call Clear()
            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            if (logEntriesType == null)
            {
                // Fallback for some Unity versions
                var assembly = Assembly.GetAssembly(typeof(EditorWindow));
                logEntriesType = assembly?.GetType("UnityEditorInternal.LogEntries");
            }

            if (logEntriesType == null)
            {
                UnityEngine.Debug.LogWarning("⚠️ Cannot find LogEntries type via reflection.");
                return;
            }

            // Get the Clear method
            var clearMethod = logEntriesType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            if (clearMethod == null)
            {
                UnityEngine.Debug.LogWarning("⚠️ Cannot find Clear method via reflection.");
                return;
            }

            clearMethod.Invoke(null, null);
#endif
        }
    }
}
