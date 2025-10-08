using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Tools;

namespace WorldSpawnerTool
{
    /// <summary>
    /// Main Editor Window - Pure UI orchestration, delegates logic to specialized classes
    /// </summary>
    public class WorldSpawnerWindow : EditorWindow
    {
        // ═══════════════════════════════════════════════════════════
        // DATA
        // ═══════════════════════════════════════════════════════════
        private GameObject prefabToSpawn;
        private SpawnSettings spawnSettings = new SpawnSettings();
        private PreviewSettings previewSettings = new PreviewSettings();
        private PaintSettings paintSettings = new PaintSettings();
    
        // ═══════════════════════════════════════════════════════════
        // COMPONENTS (Logic Classes)
        // ═══════════════════════════════════════════════════════════
        private SpawnPointManager pointManager = new SpawnPointManager();
        private SpawnPointPainter painter;
        private List<GameObject> allSpawnedObjects = new List<GameObject>();
    
        // ═══════════════════════════════════════════════════════════
        // UI STATE
        // ═══════════════════════════════════════════════════════════
        private Vector2 scrollPosition;
        private bool showPaintModeFoldout = true;
        private bool showSpawnPointsList = true;
        private bool showSpawnOptions = true;
        private bool showPreviewOptions = true;
        
        // Optimization flags
        private bool isDirty = false;
    
        // ═══════════════════════════════════════════════════════════
        // UNITY METHODS
        // ═══════════════════════════════════════════════════════════
    
        [MenuItem("Tools/World Spawner")]
        public static void ShowWindow()
        {
            WorldSpawnerWindow window = GetWindow<WorldSpawnerWindow>("World Spawner - NguyenNP");
            window.minSize = new Vector2(300, 400);
        }
    
        private void OnEnable()
        {
            // Initialize painter with settings reference
            painter = new SpawnPointPainter(paintSettings);
            painter.OnPointCreated += OnPainterCreatedPoint;
            painter.OnPointErased += OnPainterErasedPoint;
    
            // Subscribe to Unity events
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
            Selection.selectionChanged += OnSelectionChanged;
        }
    
        private void OnDisable()
        {
            // Unsubscribe from events
            if (painter != null)
            {
                painter.OnPointCreated -= OnPainterCreatedPoint;
                painter.OnPointErased -= OnPainterErasedPoint;
            }
    
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Selection.selectionChanged -= OnSelectionChanged;
        }
    
        // ═══════════════════════════════════════════════════════════
        // UI (OnGUI)
        // ═══════════════════════════════════════════════════════════
    
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
    
            // ─────────────────────────────────────────────────────
            // PREFAB SELECTION (Always visible)
            // ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Prefab to Spawn:", EditorStyles.boldLabel, GUILayout.Height(20));
            
            EditorGUI.BeginChangeCheck();
            prefabToSpawn = (GameObject)EditorGUILayout.ObjectField(
                prefabToSpawn,
                typeof(GameObject),
                false,
                GUILayout.Height(30)
            );
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }
    
            EditorGUILayout.Space(10);
    
            // ─────────────────────────────────────────────────────
            // PAINT MODE FOLDOUT
            // ─────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            showPaintModeFoldout = WorldSpawnerUI.DrawPaintModeUI(paintSettings, showPaintModeFoldout);
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }
    
            // ─────────────────────────────────────────────────────
            // SPAWN POINTS LIST FOLDOUT
            // ─────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            showSpawnPointsList = WorldSpawnerUI.DrawSpawnPointsListUI(
                pointManager,
                showSpawnPointsList,
                onAddEmpty: () => pointManager.AddEmpty(),
                onAddSelected: AddSelectedObjects,
                onClearAll: () => { pointManager.Clear(); painter.Reset(); }
            );
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }
    
            // ─────────────────────────────────────────────────────
            // SPAWN OPTIONS FOLDOUT
            // ─────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            showSpawnOptions = WorldSpawnerUI.DrawSpawnOptionsUI(spawnSettings, showSpawnOptions);
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }
    
            // ─────────────────────────────────────────────────────
            // PREVIEW OPTIONS FOLDOUT
            // ─────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            showPreviewOptions = WorldSpawnerUI.DrawPreviewOptionsUI(previewSettings, showPreviewOptions);
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }
    
            EditorGUILayout.Space(10);
    
            // ─────────────────────────────────────────────────────
            // ACTION BUTTONS
            // ─────────────────────────────────────────────────────
            DrawActionButtons();
    
            EditorGUILayout.Space();
    
            // ─────────────────────────────────────────────────────
            // INFO DISPLAY
            // ─────────────────────────────────────────────────────
            EditorGUILayout.HelpBox(
                $"Points: {pointManager.Count} | Spawned: {allSpawnedObjects.Count}",
                MessageType.Info
            );
    
            EditorGUILayout.EndScrollView();
    
            // Only repaint scene view when settings actually changed
            if (isDirty)
            {
                SceneView.RepaintAll();
                isDirty = false;
            }
        }
    
        /// <summary>
        /// Draw action buttons (Spawn, Delete)
        /// </summary>
        private void DrawActionButtons()
        {
            // Spawn button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("SPAWN OBJECTS", GUILayout.Height(40)))
            {
                SpawnObjects();
            }
            GUI.backgroundColor = Color.white;
    
            EditorGUILayout.Space();
    
            // Delete buttons
            EditorGUILayout.BeginHorizontal();
    
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("Delete All Spawned", GUILayout.Height(35)))
            {
                DeleteAllSpawnedObjects();
            }
    
            GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
            if (GUILayout.Button("Delete Spawn Points", GUILayout.Height(35)))
            {
                DeleteSpawnPoints();
            }
    
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
    
        // ═══════════════════════════════════════════════════════════
        // SCENE GUI
        // ═══════════════════════════════════════════════════════════
    
        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
    
            // ─────────────────────────────────────────────────────
            // SHORTCUT: Press 'P' to toggle paint mode
            // ─────────────────────────────────────────────────────
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.P)
            {
                paintSettings.isActive = !paintSettings.isActive;
                Repaint();
                sceneView.Repaint();
    
                string message = paintSettings.isActive ? "🎨 Paint Mode ON" : "Paint Mode OFF";
                sceneView.ShowNotification(new GUIContent(message), 1f);
    
                e.Use();
            }
    
            // ─────────────────────────────────────────────────────
            // PAINT MODE (has priority)
            // ─────────────────────────────────────────────────────
            painter.OnSceneGUI(sceneView);
    
            // ─────────────────────────────────────────────────────
            // PREVIEW RENDERING (Always show unless disabled in settings)
            // ─────────────────────────────────────────────────────
            SpawnPreviewRenderer.DrawPreview(pointManager.Points, previewSettings, spawnSettings);
    
            // No need to force repaint here - let painter and preview handle their own repaints
        }
    
        // ═══════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════
    
        private void OnUndoRedo()
        {
            // Auto cleanup missing references after undo/redo
            pointManager.RemoveNullPoints();
            
            // Mark painter cache as dirty
            painter.MarkCacheDirty();
            
            Repaint();
            SceneView.RepaintAll();
        }
    
        private void OnSelectionChanged()
        {
            // Only repaint if necessary (when selection might affect spawn points)
            Repaint();
        }
    
        private void OnPainterCreatedPoint(Transform newPoint)
        {
            pointManager.Add(newPoint);
            Repaint();
        }
    
        private void OnPainterErasedPoint(Transform erasedPoint)
        {
            // Remove from point manager if exists
            int index = pointManager.IndexOf(erasedPoint);
            if (index >= 0)
            {
                pointManager.Remove(index);
                Repaint();
            }
        }
    
        // ═══════════════════════════════════════════════════════════
        // ACTIONS (Delegate to logic classes)
        // ═══════════════════════════════════════════════════════════
    
        private void AddSelectedObjects()
        {
            if (Selection.transforms.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select objects in the scene first!", "OK");
                return;
            }
    
            int addedCount = pointManager.AddRange(Selection.transforms);
            Debug.Log($"Added {addedCount} spawn point(s)");
        }
    
        private void SpawnObjects()
        {
            // Disable paint mode during spawn
            paintSettings.isActive = false;
    
            // Validation
            if (prefabToSpawn == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a prefab to spawn!", "OK");
                return;
            }
    
            if (pointManager.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please add at least one spawn point!", "OK");
                return;
            }
    
            // Delegate to ObjectSpawner
            SpawnResult result = ObjectSpawner.SpawnObjects(prefabToSpawn, pointManager.Points, spawnSettings);
    
            if (result != null)
            {
                // Track spawned objects
                allSpawnedObjects.AddRange(result.SpawnedObjects);
    
                // Show result
                EditorUtility.DisplayDialog("Spawn Complete", result.GetMessage(), "OK");
            }
        }
    
        private void DeleteAllSpawnedObjects()
        {
            // Clean up null references
            allSpawnedObjects.RemoveAll(obj => obj == null);
    
            if (allSpawnedObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("No Objects", "No spawned objects to delete!", "OK");
                return;
            }
    
            // Confirm
            if (!EditorUtility.DisplayDialog(
                "Delete All Spawned Objects",
                $"Delete ALL {allSpawnedObjects.Count} spawned objects?\n\nThis can be undone with Ctrl+Z.",
                "Yes, Delete All",
                "Cancel"))
            {
                return;
            }
    
            // Delegate to ObjectSpawner
            int deletedCount = ObjectSpawner.DeleteObjects(allSpawnedObjects);
    
            if (deletedCount > 0)
            {
                EditorUtility.DisplayDialog("Deleted", $"Successfully deleted {deletedCount} spawned object(s)!", "OK");
            }
            // Clear Log Console
            ClearLogConsole.Clear();
        }
    
        private void DeleteSpawnPoints()
        {
            if (pointManager.Count == 0)
            {
                if (EditorUtility.DisplayDialog(
                    "No Spawn Points in List",
                    "No spawn points in the list, but there might still be leftover SpawnPoint objects in the Scene.\n\nDo you want to delete all objects whose names start with the prefix?",
                    "Yes, Delete by Prefix",
                    "Cancel"))
                {
                    int deletedCByPrefixCount = ObjectSpawner.DeleteSpawnPoints(null, paintSettings);
                    if (deletedCByPrefixCount > 0)
                    {
                        EditorUtility.DisplayDialog("Deleted", $"Deleted {deletedCByPrefixCount} leftover SpawnPoint objects.", "OK");
                        painter.MarkCacheDirty();
                    }
                }
                return;
            }
    
    
            int validCount = pointManager.GetValidCount();
    
            if (validCount == 0)
            {
                EditorUtility.DisplayDialog("No Valid Spawn Points", "All spawn points are already null!", "OK");
                pointManager.Clear();
                return;
            }
    
            // Confirm
            if (!EditorUtility.DisplayDialog(
                "Delete Spawn Points",
                $"Delete {validCount} spawn point GameObject(s) from scene?\n\nThis can be undone with Ctrl+Z.",
                "Yes, Delete",
                "Cancel"))
            {
                return;
            }
    
            // Delegate to ObjectSpawner
            int deletedCount = ObjectSpawner.DeleteSpawnPoints(pointManager.Points, paintSettings);
    
            // Clear the list and mark cache dirty
            pointManager.Clear();
            painter.MarkCacheDirty();
    
            if (deletedCount > 0)
            {
                EditorUtility.DisplayDialog("Deleted", $"Successfully deleted {deletedCount} spawn point(s) from scene!", "OK");
            }
            // Clear Log Console
            ClearLogConsole.Clear();
        }
    }
}