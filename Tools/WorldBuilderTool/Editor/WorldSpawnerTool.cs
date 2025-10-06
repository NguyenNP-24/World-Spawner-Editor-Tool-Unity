using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;

public class WorldSpawnerTool : EditorWindow
{
    private GameObject prefabToSpawn;
    private List<Transform> spawnPoints = new List<Transform>();
    private List<GameObject> allSpawnedObjects = new List<GameObject>(); // Track all spawned objects
    private Vector2 scrollPosition;
    private bool showSpawnPointsList = true;

    /// Track spawn point by alphabetical
    private Dictionary<Transform, int> spawnPointIndices = new Dictionary<Transform, int>();

    // Spawn Options
    private bool useRandomRotation = false;
    private bool randomRotationX = false;
    private bool randomRotationY = true;
    private bool randomRotationZ = false;
    private bool useRandomScale = false;
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    private bool alignToSurface = false;
    private LayerMask surfaceLayerMask = -1; // Default: Everything
    private float raycastDistance = 20f;
    private bool parentToSpawnPoint = false;
    private string objectNamePrefix = ""; // If empty, use prefab name

    // Preview Options
    private bool showPreview = true;
    private bool showNames = true;
    private bool showNameBackground = false;
    private bool showDirections = true;
    private Color previewColor = new Color(1.0f, 0f, 0.13f, 0.6f); // Semi-transparent red
    private float previewSphereSize = 0.3f;
    private float previewArrowSize = 1f;

    [MenuItem("Tools/World Spawner")]
    public static void ShowWindow()
    {
        WorldSpawnerTool window = GetWindow<WorldSpawnerTool>("World Spawner");
        window.minSize = new Vector2(300, 400);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreview || spawnPoints.Count == 0)
            return;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
                continue;

            Vector3 position = spawnPoint.position;
            Quaternion rotation = spawnPoint.rotation;

            // Calculate preview rotation (only for align to surface, NOT random)
            if (alignToSurface)
            {
                RaycastHit hit;

                if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, raycastDistance + 10f, surfaceLayerMask))
                {
                    rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                    // Draw surface normal (cyan)
                    Handles.color = Color.cyan;
                    Handles.DrawLine(hit.point, hit.point + hit.normal * 1.5f);

                    // Draw successful raycast (green)
                    Handles.color = new Color(0f, 1f, 0f, 0.5f);
                    Handles.DrawLine(position + Vector3.up * 10f, hit.point);

                    // Draw hit point
                    Handles.color = Color.green;
                    Handles.SphereHandleCap(0, hit.point, Quaternion.identity, 0.2f, EventType.Repaint);
                }
                else
                {
                    // Draw failed raycast (red)
                    Handles.color = new Color(1f, 0f, 0f, 0.5f);
                    Handles.DrawLine(position + Vector3.up * 10f, position + Vector3.down * raycastDistance);
                }
            }

            // Note: Random rotation is NOT previewed because it's calculated at spawn time

            // Draw sphere at spawn point with preview color
            Handles.color = previewColor;
            Handles.SphereHandleCap(0, position, Quaternion.identity, previewSphereSize, EventType.Repaint);

            // Draw orientation arrows
            if (showDirections)
            {
                float size = previewArrowSize;

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

            // Draw label
            if (showNames)
            {
                GUIStyle style = new GUIStyle(EditorStyles.whiteBoldLabel);
                style.normal.textColor = Color.white;
                style.fontSize = 11;

                if (showNameBackground)
                {
                    // Create background texture
                    style.normal.background = MakeBackgroundTexture(2, 2, new Color(0f, 0f, 0f, 0.7f));
                    style.padding = new RectOffset(4, 4, 2, 2);
                }

                Handles.Label(position + Vector3.up * 0.5f, spawnPoint.name, style);
            }
        }

        // Only repaint if we're not in repaint event
        if (Event.current.type != EventType.Repaint)
        {
            sceneView.Repaint();
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space();

        // Prefab Selection
        EditorGUILayout.LabelField("Prefab to Spawn:", EditorStyles.boldLabel, GUILayout.Height(20));
        prefabToSpawn = (GameObject)EditorGUILayout.ObjectField(prefabToSpawn, typeof(GameObject), false, GUILayout.Height(30));

        EditorGUILayout.Space();

        // Spawn Points Section with Foldout
        showSpawnPointsList = EditorGUILayout.Foldout(showSpawnPointsList, $"Spawn Points ({spawnPoints.Count}):", true, EditorStyles.foldoutHeader);

        EditorGUILayout.Space();

        if (showSpawnPointsList)
        {
            EditorGUILayout.HelpBox("Add spawn points manually or use Add selected objects from scene.", MessageType.Info);

            // Buttons for managing spawn points
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Empty Points"))
            {
                spawnPoints.Add(null);
            }

            if (GUILayout.Button("Add Selected Points"))
            {
                AddSelectedObjects();
            }

            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Remove all spawn points?", "Yes", "No"))
                {
                    spawnPoints.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Display spawn points list
            if (spawnPoints.Count == 0)
            {
                EditorGUILayout.HelpBox("No spawn points added yet.", MessageType.Warning);
            }
            else
            {
                int removeIndex = -1;

                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    spawnPoints[i] = (Transform)EditorGUILayout.ObjectField($"Point {i + 1}", spawnPoints[i], typeof(Transform), true);

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        removeIndex = i;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // Remove after the loop to avoid GUI errors
                if (removeIndex >= 0)
                {
                    spawnPoints.RemoveAt(removeIndex);
                    GUI.FocusControl(null); // Clear focus to avoid GUI state issues
                    Repaint();
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn Options:", EditorStyles.boldLabel);

        // Spawn options with indent
        EditorGUI.indentLevel++;

        objectNamePrefix = EditorGUILayout.TextField("Object Name Prefix", objectNamePrefix);

        alignToSurface = EditorGUILayout.Toggle("Align to Surface", alignToSurface);
        if (alignToSurface)
        {
            EditorGUI.indentLevel++;
            surfaceLayerMask = EditorGUILayout.MaskField("Surface Layers",
                InternalEditorUtility.LayerMaskToConcatenatedLayersMask(surfaceLayerMask),
                InternalEditorUtility.layers);
            surfaceLayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(surfaceLayerMask);

            raycastDistance = EditorGUILayout.Slider("Raycast Distance", raycastDistance, 5f, 100f);
            EditorGUILayout.HelpBox("Select layers to use as surface. Green line = hit, Red line = no hit.", MessageType.Info);
            EditorGUI.indentLevel--;
        }

        useRandomRotation = EditorGUILayout.Toggle("Random Rotation", useRandomRotation);
        if (useRandomRotation)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            randomRotationX = EditorGUILayout.ToggleLeft("X Axis", randomRotationX, GUILayout.Width(90));
            randomRotationY = EditorGUILayout.ToggleLeft("Y Axis", randomRotationY, GUILayout.Width(90));
            randomRotationZ = EditorGUILayout.ToggleLeft("Z Axis", randomRotationZ, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            if (alignToSurface)
            {
                EditorGUILayout.HelpBox("Random rotation will be applied AFTER surface alignment.", MessageType.Info);
            }
            EditorGUI.indentLevel--;
        }

        useRandomScale = EditorGUILayout.Toggle("Random Scale", useRandomScale);
        if (useRandomScale)
        {
            EditorGUILayout.BeginHorizontal();

            // Giữ nguyên label chính, có thể dùng indent nếu muốn
            EditorGUILayout.LabelField("     Scale Range:", GUILayout.Width(EditorGUIUtility.labelWidth - 4));

            // Reset indent tạm thời để chữ Min/Max không bị đẩy vào
            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Nhóm Min
            EditorGUILayout.LabelField("Min: ", GUILayout.Width(25));
            scaleRange.x = EditorGUILayout.FloatField(scaleRange.x, GUILayout.Width(50));

            // Giữa Min và Max có khoảng trống
            GUILayout.Label(" - ", GUILayout.Width(15));

            // Nhóm Max
            EditorGUILayout.LabelField("Max: ", GUILayout.Width(25));
            scaleRange.y = EditorGUILayout.FloatField(scaleRange.y, GUILayout.Width(50));

            // Trả indent lại
            EditorGUI.indentLevel = prevIndent;

            EditorGUILayout.EndHorizontal();


        }

        parentToSpawnPoint = EditorGUILayout.Toggle("Parent to Spawn Point", parentToSpawnPoint);

        EditorGUI.indentLevel--; // Reset indent after all spawn options

        EditorGUILayout.Space();

        // Preview Options
        EditorGUILayout.LabelField("Preview Options:", EditorStyles.boldLabel);
        showPreview = EditorGUILayout.Toggle("Show Scene Preview", showPreview);
        if (showPreview)
        {
            EditorGUI.indentLevel++;

            // Show Names and Show Background in same line
            EditorGUILayout.BeginHorizontal();
            showNames = EditorGUILayout.Toggle("Show Names", showNames);

            GUI.enabled = showNames; // Disable if showNames is false
            showNameBackground = EditorGUILayout.Toggle("Show Background", showNameBackground);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            showDirections = EditorGUILayout.Toggle("Show Directions", showDirections);
            previewColor = EditorGUILayout.ColorField("Sphere Color", previewColor);
            previewSphereSize = EditorGUILayout.Slider("Sphere Size", previewSphereSize, 0.1f, 10f);
            if (showDirections)
            {
                previewArrowSize = EditorGUILayout.Slider("Arrow Size", previewArrowSize, 0.5f, 5f);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Spawn button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("SPAWN OBJECTS", GUILayout.Height(40)))
        {
            SpawnObjects();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // Delete buttons in one row
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("Delete All Spawned Objects", GUILayout.Height(35)))
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

        EditorGUILayout.Space();

        // Info display
        EditorGUILayout.HelpBox($"Spawn Points: {spawnPoints.Count} | Tracked Objects: {allSpawnedObjects.Count}", MessageType.Info);

        EditorGUILayout.EndScrollView();

        // Force repaint scene view when settings change
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    private void AddSelectedObjects()
    {
        if (Selection.transforms.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select objects in the scene first!", "OK");
            return;
        }

        int addedCount = 0;

        // Sort selected transforms by name for consistent ordering
        var sortedTransforms = Selection.transforms.OrderBy(t => t.name).ToList();

        foreach (Transform t in sortedTransforms)
        {
            if (!spawnPoints.Contains(t))
            {
                spawnPoints.Add(t);
                addedCount++;
            }
        }

        Debug.Log($"Added {addedCount} spawn point(s)");
    }

    private void SpawnObjects()
    {
        // Determine final prefix
        string finalPrefix = string.IsNullOrEmpty(objectNamePrefix)
            ? prefabToSpawn.name + "_"
            : objectNamePrefix;

        // Validation
        if (prefabToSpawn == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a prefab to spawn!", "OK");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please add at least one spawn point!", "OK");
            return;
        }

        // Build dictionary for spawn point indices
        spawnPointIndices.Clear();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            spawnPointIndices[spawnPoints[i]] = i;
        }

        // Group all spawns into single Undo operation
        Undo.SetCurrentGroupName("Spawn Objects");
        int undoGroup = Undo.GetCurrentGroup();

        int spawnedCount = 0;
        int skippedCount = 0;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
            {
                skippedCount++;
                continue;
            }

            // Instantiate the object
            GameObject spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);

            // Set position
            spawnedObject.transform.position = spawnPoint.position;

            // Calculate final rotation
            Quaternion finalRotation = spawnPoint.rotation;

            // Step 1: Align to surface if enabled
            if (alignToSurface)
            {
                RaycastHit hit;
                if (Physics.Raycast(spawnPoint.position + Vector3.up * 10f, Vector3.down, out hit, raycastDistance + 10f, surfaceLayerMask))
                {
                    finalRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(spawnPoint.forward, hit.normal), hit.normal);
                }
            }

            // Step 2: Apply random rotation
            if (useRandomRotation)
            {
                Vector3 randomEuler = finalRotation.eulerAngles;
                if (randomRotationX) randomEuler.x = Random.Range(0f, 360f);
                if (randomRotationY) randomEuler.y = Random.Range(0f, 360f);
                if (randomRotationZ) randomEuler.z = Random.Range(0f, 360f);
                finalRotation = Quaternion.Euler(randomEuler);
            }

            spawnedObject.transform.rotation = finalRotation;

            // Scale
            if (useRandomScale)
            {
                float randomScale = Random.Range(scaleRange.x, scaleRange.y);
                spawnedObject.transform.localScale = Vector3.one * randomScale;
            }

            // Parent
            if (parentToSpawnPoint)
                spawnedObject.transform.SetParent(spawnPoint);

            // Name
            int pointIndex = spawnPointIndices.ContainsKey(spawnPoint)
                ? spawnPointIndices[spawnPoint]
                : spawnedCount;
            spawnedObject.name = finalPrefix + pointIndex;

            // Register undo + track
            allSpawnedObjects.Add(spawnedObject);
            Undo.RegisterCreatedObjectUndo(spawnedObject, "Spawn Objects");

            spawnedCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        // Result
        string message = $"Successfully spawned {spawnedCount} object(s)!";
        if (skippedCount > 0)
            message += $"\nSkipped {skippedCount} empty spawn point(s).";

        EditorUtility.DisplayDialog("Spawn Complete", message, "OK");
        Debug.Log($"[World Spawner] {message}");
    }

    private void DeleteAllSpawnedObjects()
    {
        // Clean up null references first
        allSpawnedObjects.RemoveAll(obj => obj == null);

        if (allSpawnedObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("No Objects", "No spawned objects to delete!\n\nMake sure you spawned objects using this tool window.", "OK");
            return;
        }

        // Confirm deletion
        if (!EditorUtility.DisplayDialog(
            "Delete All Spawned Objects",
            $"This will delete ALL {allSpawnedObjects.Count} objects that were spawned using this tool (from all spawn sessions).\n\nThis action can be undone with Ctrl+Z.\n\nAre you sure?",
            "Yes, Delete All",
            "Cancel"))
        {
            return;
        }

        // Group deletion into single Undo operation
        Undo.SetCurrentGroupName("Delete All Spawned Objects");
        int undoGroup = Undo.GetCurrentGroup();

        int deletedCount = 0;
        for (int i = allSpawnedObjects.Count - 1; i >= 0; i--)
        {
            if (allSpawnedObjects[i] != null)
            {
                Undo.DestroyObjectImmediate(allSpawnedObjects[i]);
                deletedCount++;
            }
        }

        // Clear the tracking list
        allSpawnedObjects.Clear();

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Deleted", $"Successfully deleted {deletedCount} spawned object(s)!", "OK");
        Debug.Log($"[World Spawner] Deleted {deletedCount} spawned objects");
    }

    private void DeleteSpawnPoints()
    {
        if (spawnPoints.Count == 0)
        {
            EditorUtility.DisplayDialog("No Spawn Points", "No spawn points in the list to delete!", "OK");
            return;
        }

        // Count non-null spawn points
        int validCount = spawnPoints.Count(sp => sp != null);

        if (validCount == 0)
        {
            EditorUtility.DisplayDialog("No Valid Spawn Points", "All spawn points in the list are already null!", "OK");
            spawnPoints.Clear();
            return;
        }

        // Confirm deletion
        if (!EditorUtility.DisplayDialog(
            "Delete Spawn Points",
            $"This will delete {validCount} spawn point GameObject(s) from the scene.\n\nThis action can be undone with Ctrl+Z.\n\nAre you sure?",
            "Yes, Delete",
            "Cancel"))
        {
            return;
        }

        // Group deletion into single Undo operation
        Undo.SetCurrentGroupName("Delete Spawn Points");
        int undoGroup = Undo.GetCurrentGroup();

        int deletedCount = 0;
        for (int i = spawnPoints.Count - 1; i >= 0; i--)
        {
            if (spawnPoints[i] != null)
            {
                Undo.DestroyObjectImmediate(spawnPoints[i].gameObject);
                deletedCount++;
            }
        }

        // Clear the spawn points list
        spawnPoints.Clear();

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Deleted", $"Successfully deleted {deletedCount} spawn point(s) from scene!", "OK");
        Debug.Log($"[World Spawner] Deleted {deletedCount} spawn points");
    }

    // Helper method to create background texture for labels
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
