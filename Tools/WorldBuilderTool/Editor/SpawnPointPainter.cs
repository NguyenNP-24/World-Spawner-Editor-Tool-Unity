using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WorldSpawnerTool
{
    /// <summary>
    /// Paint mode logic for creating spawn points - Pure logic, no UI settings drawing
    /// </summary>
    public class SpawnPointPainter
    {
        // Settings reference (managed externally)
        public PaintSettings Settings { get; set; }

        // Internal state
        private Vector3 lastPaintPosition;
        private bool isPainting;
        private bool isErasing;
        private int pointCounter = 0;

        // Constant for raycast distance
        private const float PAINT_RAYCAST_DISTANCE = 5000f;

        // Cache for spawn points (optimization for erase mode)
        private List<Transform> cachedSpawnPoints = new List<Transform>();
        private bool needsCacheRefresh = true;

        // Event when a point is created
        public event System.Action<Transform> OnPointCreated;
        public event System.Action<Transform> OnPointErased;

        public SpawnPointPainter(PaintSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Main update method - call this in OnSceneGUI
        /// </summary>
        public void OnSceneGUI(SceneView sceneView)
        {
            if (!Settings.isActive)
                return;

            Event e = Event.current;

            // Check if in erase mode
            bool eraseMode = e.control && e.shift;

            // Change cursor to indicate paint/erase mode
            EditorGUIUtility.AddCursorRect(
                new Rect(0, 0, sceneView.position.width, sceneView.position.height),
                eraseMode ? MouseCursor.ArrowMinus : MouseCursor.ArrowPlus
            );

            // Block default scene view controls when in paint mode
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            // Cast ray from mouse position
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;

            bool hitSurface = Physics.Raycast(ray, out hit, PAINT_RAYCAST_DISTANCE, Settings.surfaceLayerMask);

            if (hitSurface)
            {
                // Draw preview cursor at hit point
                if (Settings.showCursor)
                {
                    DrawCursor(hit.point, hit.normal, eraseMode);
                }

                // Handle mouse input
                HandleInput(e, hit, eraseMode);
            }

            // Draw UI overlay
            DrawUIOverlay(eraseMode);

            // Only repaint when necessary (when user is interacting or cursor moving)
            if (hitSurface || isPainting || isErasing)
            {
                sceneView.Repaint();
            }
        }

        /// <summary>
        /// Handle mouse input for painting/erasing
        /// </summary>
        private void HandleInput(Event e, RaycastHit hit, bool eraseMode)
        {
            if (eraseMode)
            {
                // ERASE MODE: Ctrl+Shift + Left Click
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    EraseSpawnPoint(hit.point);
                    lastPaintPosition = hit.point;
                    isErasing = true;
                    e.Use();
                }
                // Continue erasing: Ctrl+Shift + Drag
                else if (e.type == EventType.MouseDrag && e.button == 0 && isErasing)
                {
                    float distance = Vector3.Distance(lastPaintPosition, hit.point);

                    // Only erase if far enough from last position
                    if (distance >= Settings.pointSpacing)
                    {
                        EraseSpawnPoint(hit.point);
                        lastPaintPosition = hit.point;
                    }
                    e.Use();
                }
                // Stop erasing
                else if (e.type == EventType.MouseUp)
                {
                    isErasing = false;
                }
            }
            else
            {
                // PAINT MODE: Shift + Left Click
                if (e.shift && e.type == EventType.MouseDown && e.button == 0)
                {
                    CreateSpawnPoint(hit.point, hit.normal);
                    lastPaintPosition = hit.point;
                    isPainting = true;
                    e.Use();
                }
                // Continue painting: Shift + Drag
                else if (e.shift && e.type == EventType.MouseDrag && e.button == 0 && isPainting)
                {
                    float distance = Vector3.Distance(lastPaintPosition, hit.point);

                    // Only create new point if far enough from last point
                    if (distance >= Settings.pointSpacing)
                    {
                        CreateSpawnPoint(hit.point, hit.normal);
                        lastPaintPosition = hit.point;
                    }
                    e.Use();
                }
                // Stop painting
                else if (e.type == EventType.MouseUp)
                {
                    isPainting = false;
                }
            }
        }

        /// <summary>
        /// Create a spawn point GameObject at specified position
        /// </summary>
        private void CreateSpawnPoint(Vector3 position, Vector3 normal)
        {
            pointCounter++;

            GameObject spawnPointObj = new GameObject($"{Settings.pointNamePrefix}_{pointCounter}");
            spawnPointObj.transform.position = position;

            // Set rotation based on surface normal
            if (Settings.alignToNormal)
            {
                Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized;
                if (forward == Vector3.zero)
                    forward = Vector3.ProjectOnPlane(Vector3.right, normal).normalized;

                spawnPointObj.transform.rotation = Quaternion.LookRotation(forward, normal);
            }

            // Register undo
            Undo.RegisterCreatedObjectUndo(spawnPointObj, "Paint Spawn Point");

            // Mark cache as dirty
            needsCacheRefresh = true;

            // Trigger event
            OnPointCreated?.Invoke(spawnPointObj.transform);

            Debug.Log($"[SpawnPointPainter] Created spawn point at {position}");
        }

        /// <summary>
        /// Erase spawn point at specified position (find closest within radius)
        /// </summary>
        private void EraseSpawnPoint(Vector3 position)
        {
            // Refresh cache if needed
            if (needsCacheRefresh)
            {
                RefreshSpawnPointsCache();
            }

            // Find closest point within cursor radius
            Transform closestPoint = null;
            float closestDistance = Settings.cursorSize;

            // Clean up destroyed objects while searching
            for (int i = cachedSpawnPoints.Count - 1; i >= 0; i--)
            {
                Transform t = cachedSpawnPoints[i];

                // Remove destroyed/null references
                if (t == null)
                {
                    cachedSpawnPoints.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(t.position, position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = t;
                }
            }

            if (closestPoint != null)
            {
                // Save name BEFORE destroying
                string pointName = closestPoint.name;

                // Register undo
                Undo.DestroyObjectImmediate(closestPoint.gameObject);

                // Remove from cache immediately
                cachedSpawnPoints.Remove(closestPoint);

                // Trigger event (before object is fully destroyed)
                OnPointErased?.Invoke(closestPoint);

                Debug.Log($"[SpawnPointPainter] Erased spawn point: {pointName}");
            }
        }

        /// <summary>
        /// Refresh cached spawn points list (only when needed)
        /// </summary>
        private void RefreshSpawnPointsCache()
        {
            cachedSpawnPoints.Clear();
            var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();

            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith(Settings.pointNamePrefix))
                {
                    cachedSpawnPoints.Add(t);
                }
            }

            needsCacheRefresh = false;
        }

        /// <summary>
        /// Draw cursor preview at mouse position
        /// </summary>
        private void DrawCursor(Vector3 position, Vector3 normal, bool eraseMode)
        {
            Color cursorColor = eraseMode ? Settings.eraseCursorColor : Settings.cursorColor;
            
            // Draw filled disc
            Handles.color = cursorColor;
            Handles.DrawSolidDisc(position, normal, Settings.cursorSize);

            // Draw wire outline
            Handles.color = new Color(
                cursorColor.r,
                cursorColor.g,
                cursorColor.b,
                1f
            );
            Handles.DrawWireDisc(position, normal, Settings.cursorSize);

            // Draw normal line
            if (Settings.alignToNormal && !eraseMode)
            {
                Handles.color = Color.cyan;
                Handles.DrawLine(position, position + normal * (Settings.cursorSize * 2f));
            }
        }

        /// <summary>
        /// Draw UI overlay with instructions
        /// </summary>
        private void DrawUIOverlay(bool eraseMode)
        {
            Handles.BeginGUI();

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeBackgroundTexture(2, 2, new Color(0f, 0f, 0f, 0.6f));
            boxStyle.padding = new RectOffset(10, 10, 5, 5);

            GUIStyle textStyle = new GUIStyle(EditorStyles.whiteLargeLabel);
            textStyle.fontSize = 12;
            textStyle.normal.textColor = Color.white;
            textStyle.fontStyle = FontStyle.Bold;

            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.BeginVertical(boxStyle);

            if (eraseMode)
            {
                GUILayout.Label("🧹 ERASE MODE ACTIVE", textStyle);
                textStyle.fontSize = 11;
                textStyle.fontStyle = FontStyle.Normal;
                GUILayout.Label("Ctrl+Shift + Click: Erase point", textStyle);
                GUILayout.Label("Ctrl+Shift + Drag: Erase continuously", textStyle);
            }
            else
            {
                GUILayout.Label("🎨 PAINT MODE ACTIVE", textStyle);
                textStyle.fontSize = 11;
                textStyle.fontStyle = FontStyle.Normal;
                GUILayout.Label("Shift + Click: Create point", textStyle);
                GUILayout.Label("Shift + Drag: Paint continuously", textStyle);
                GUILayout.Label("Ctrl+Shift: Switch to erase mode", textStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        /// <summary>
        /// Reset painter state
        /// </summary>
        public void Reset()
        {
            isPainting = false;
            isErasing = false;
            pointCounter = 0;
            lastPaintPosition = Vector3.zero;
            needsCacheRefresh = true;
            cachedSpawnPoints.Clear();
        }

        /// <summary>
        /// Mark cache as dirty (call when spawn points change externally)
        /// </summary>
        public void MarkCacheDirty()
        {
            needsCacheRefresh = true;
        }

        /// <summary>
        /// Helper method to create background texture
        /// </summary>
        private Texture2D MakeBackgroundTexture(int width, int height, Color color)
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