using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WorldSpawnerTool
{
    /// <summary>
    /// Renders preview gizmos in Scene View - Pure rendering logic
    /// </summary>
    public static class SpawnPreviewRenderer
    {
        /// <summary>
        /// Draw preview for all spawn points
        /// </summary>
        public static void DrawPreview(
            IReadOnlyList<Transform> spawnPoints,
            PreviewSettings previewSettings,
            SpawnSettings spawnSettings)
        {
            if (!previewSettings.showPreview || spawnPoints == null || spawnPoints.Count == 0)
                return;
    
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                    continue;
    
                Vector3 position = spawnPoint.position;
                Quaternion rotation = CalculatePreviewRotation(spawnPoint, spawnSettings);
    
                // Draw sphere at spawn point
                DrawSpawnPointSphere(position, previewSettings);
    
                // Draw orientation arrows
                if (previewSettings.showDirections)
                {
                    DrawOrientationArrows(position, rotation, previewSettings);
                }
    
                // Draw label
                if (previewSettings.showNames)
                {
                    DrawSpawnPointLabel(position, spawnPoint.name, previewSettings);
                }
            }
        }
    
        /// <summary>
        /// Calculate rotation for preview (considers align to surface)
        /// </summary>
        private static Quaternion CalculatePreviewRotation(Transform spawnPoint, SpawnSettings settings)
        {
            Quaternion rotation = spawnPoint.rotation;
    
            // Only modify if align to surface is enabled (for preview visualization)
            if (settings.alignToSurface)
            {
                Vector3 position = spawnPoint.position;
                RaycastHit hit;
    
                if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 
                    settings.raycastDistanceSpawn + 10f, settings.surfaceLayerMask))
                {
                    rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
    
                    // Draw surface alignment debug
                    DrawSurfaceAlignmentDebug(position, hit, settings.raycastDistanceSpawn);
                }
                else
                {
                    // Draw failed raycast
                    DrawFailedRaycast(position, settings.raycastDistanceSpawn);
                }
            }
    
            // Note: Random rotation is NOT previewed as it's calculated at spawn time
            return rotation;
        }
    
        /// <summary>
        /// Draw sphere at spawn point position
        /// </summary>
        private static void DrawSpawnPointSphere(Vector3 position, PreviewSettings settings)
        {
            Handles.color = settings.previewColor;
            Handles.SphereHandleCap(0, position, Quaternion.identity, settings.previewSphereSize, EventType.Repaint);
        }
    
        /// <summary>
        /// Draw X, Y, Z orientation arrows
        /// </summary>
        private static void DrawOrientationArrows(Vector3 position, Quaternion rotation, PreviewSettings settings)
        {
            float size = settings.previewArrowSize;
    
            // Forward (Blue) - Z axis
            Handles.color = new Color(0f, 0.5f, 1f, 0.9f);
            Vector3 forward = rotation * Vector3.forward * size;
            Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(forward), size, EventType.Repaint);
    
            // Right (Red) - X axis
            Handles.color = new Color(1f, 0f, 0f, 0.7f);
            Vector3 right = rotation * Vector3.right * size * 0.6f;
            Handles.DrawLine(position, position + right);
            Handles.ConeHandleCap(0, position + right, Quaternion.LookRotation(right), size * 0.2f, EventType.Repaint);
    
            // Up (Green) - Y axis
            Handles.color = new Color(0f, 1f, 0f, 0.7f);
            Vector3 up = rotation * Vector3.up * size * 0.6f;
            Handles.DrawLine(position, position + up);
            Handles.ConeHandleCap(0, position + up, Quaternion.LookRotation(up), size * 0.2f, EventType.Repaint);
        }
    
        /// <summary>
        /// Draw spawn point name label
        /// </summary>
        private static void DrawSpawnPointLabel(Vector3 position, string name, PreviewSettings settings)
        {
            GUIStyle style = new GUIStyle(EditorStyles.whiteBoldLabel);
            style.normal.textColor = Color.white;
            style.fontSize = 11;
    
            if (settings.showNameBackground)
            {
                style.normal.background = MakeBackgroundTexture(2, 2, new Color(0f, 0f, 0f, 0.7f));
                style.padding = new RectOffset(4, 4, 2, 2);
            }
    
            Handles.Label(position + Vector3.up * 0.5f, name, style);
        }
    
        /// <summary>
        /// Draw surface alignment debug visualization
        /// </summary>
        private static void DrawSurfaceAlignmentDebug(Vector3 startPosition, RaycastHit hit, float raycastDistanceSpawn)
        {
            // Draw surface normal (cyan)
            Handles.color = Color.cyan;
            Handles.DrawLine(hit.point, hit.point + hit.normal * 1.5f);
    
            // Draw successful raycast (green)
            Handles.color = new Color(0f, 1f, 0f, 0.5f);
            Handles.DrawLine(startPosition + Vector3.up * 10f, hit.point);
    
            // Draw hit point
            Handles.color = Color.green;
            Handles.SphereHandleCap(0, hit.point, Quaternion.identity, 0.2f, EventType.Repaint);
        }
    
        /// <summary>
        /// Draw failed raycast (no surface hit)
        /// </summary>
        private static void DrawFailedRaycast(Vector3 startPosition, float raycastDistanceSpawn)
        {
            Handles.color = new Color(1f, 0f, 0f, 0.5f);
            Handles.DrawLine(startPosition + Vector3.up * 10f, startPosition + Vector3.down * raycastDistanceSpawn);
        }
    
        /// <summary>
        /// Helper method to create background texture for labels
        /// </summary>
        private static Texture2D MakeBackgroundTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
    
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
    
            return texture;
        }
    }
}