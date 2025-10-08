using UnityEngine;

namespace WorldSpawnerTool
{
    /// <summary>
    /// Pure data class containing all spawn-related settings
    /// </summary>
    [System.Serializable]
    public class SpawnSettings
    {
        // Rotation
        public bool useRandomRotation = false;
        public bool randomRotationX = false;
        public bool randomRotationY = true;
        public bool randomRotationZ = false;
    
        // Scale
        public bool useRandomScale = false;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
        // Surface Alignment
        public bool alignToSurface = false;
        public LayerMask surfaceLayerMask = -1; // Everything
    
        public float raycastDistanceSpawn = 20f;
    
        // Parenting & Naming
        public bool parentToSpawnPoint = false;
        public string objectNamePrefix = ""; // Empty = use prefab name
    
        /// <summary>
        /// Get final name prefix (fallback to prefab name if empty)
        /// </summary>
        public string GetFinalPrefix(GameObject prefab)
        {
            return string.IsNullOrEmpty(objectNamePrefix) 
                ? prefab.name + "_" 
                : objectNamePrefix;
        }
    }
    
    /// <summary>
    /// Pure data class containing all preview-related settings
    /// </summary>
    [System.Serializable]
    public class PreviewSettings
    {
        // General
        public bool showPreview = true;
    
        // Labels
        public bool showNames = true;
        public bool showNameBackground = false;
    
        // Directions
        public bool showDirections = true;
    
        // Visual
        public Color previewColor = new Color(1.0f, 0f, 0.13f, 0.6f); // Semi-transparent red
        public float previewSphereSize = 0.3f;
        public float previewArrowSize = 1f;
    }
    
    /// <summary>
    /// Pure data class containing all paint mode settings
    /// </summary>
    [System.Serializable]
    public class PaintSettings
    {
        // State
        public bool isActive = false;
    
        // Spacing
        public float pointSpacing = 1f;
    
        // Surface Detection
        public LayerMask surfaceLayerMask = -1; // Everything
    
        // Alignment
        public bool alignToNormal = true;
    
        // Visual
        public bool showCursor = true;
        public float cursorSize = 0.5f;
        public Color cursorColor = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
        public Color eraseCursorColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
    
        // Naming
        public string pointNamePrefix = "SpawnPoint";
    }
}