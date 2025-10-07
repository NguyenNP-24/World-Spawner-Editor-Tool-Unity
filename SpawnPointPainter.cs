using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Paint mode tool for creating spawn points by clicking on surfaces in Scene View
/// </summary>
public class SpawnPointPainter
{
    // Settings
    public bool IsActive { get; set; }
    public float PointSpacing { get; set; } = 1f;
    public LayerMask SurfaceLayerMask { get; set; } = -1;
    public float RaycastDistance { get; set; } = 1000f;
    public bool ShowCursor { get; set; } = true;
    public float CursorSize { get; set; } = 0.5f;
    public Color CursorColor { get; set; } = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
    public bool AlignToNormal { get; set; } = true;
    public string PointNamePrefix { get; set; } = "SpawnPoint";
    private bool showPaintModeOptions = true;
    // Internal state
    private Vector3 lastPaintPosition;
    private bool isPainting;
    private int pointCounter = 0;

    // Callback when a point is created
    public System.Action<Transform> OnPointCreated;

    /// <summary>
    /// Main update method - call this in OnSceneGUI
    /// </summary>
    public void OnSceneGUI(SceneView sceneView)
    {
        if (!IsActive)
            return;

        Event e = Event.current;

        // Change cursor to indicate paint mode
        EditorGUIUtility.AddCursorRect(
            new Rect(0, 0, sceneView.position.width, sceneView.position.height),
            MouseCursor.ArrowPlus
        );

        // Block default scene view controls when in paint mode
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        // Cast ray from mouse position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, RaycastDistance, SurfaceLayerMask))
        {
            // Draw preview cursor at hit point
            if (ShowCursor)
            {
                DrawCursor(hit.point, hit.normal);
            }

            sceneView.Repaint();

            // Handle mouse input
            HandleInput(e, hit);
        }

        // Draw UI overlay
        DrawUIOverlay();
    }

    /// <summary>
    /// Handle mouse input for painting
    /// </summary>
    private void HandleInput(Event e, RaycastHit hit)
    {
        // Start painting: Shift + Left Click
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
            if (distance >= PointSpacing)
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

    /// <summary>
    /// Create a spawn point GameObject at specified position
    /// </summary>
    private void CreateSpawnPoint(Vector3 position, Vector3 normal)
    {
        pointCounter++;

        GameObject spawnPointObj = new GameObject($"{PointNamePrefix}_{pointCounter}");
        spawnPointObj.transform.position = position;

        // Set rotation based on surface normal
        if (AlignToNormal)
        {
            // Align up axis with normal, keep forward pointing in world forward direction
            Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized;
            if (forward == Vector3.zero)
                forward = Vector3.ProjectOnPlane(Vector3.right, normal).normalized;

            spawnPointObj.transform.rotation = Quaternion.LookRotation(forward, normal);
        }

        // Register undo
        Undo.RegisterCreatedObjectUndo(spawnPointObj, "Paint Spawn Point");

        // Trigger callback
        OnPointCreated?.Invoke(spawnPointObj.transform);

        Debug.Log($"[SpawnPointPainter] Created spawn point at {position}");
    }

    /// <summary>
    /// Draw cursor preview at mouse position
    /// </summary>
    private void DrawCursor(Vector3 position, Vector3 normal)
    {
        // Draw filled disc
        Handles.color = CursorColor;
        Handles.DrawSolidDisc(position, normal, CursorSize);

        // Draw wire outline
        Handles.color = new Color(CursorColor.r, CursorColor.g, CursorColor.b, 1f);
        Handles.DrawWireDisc(position, normal, CursorSize);

        // Draw normal line
        if (AlignToNormal)
        {
            Handles.color = Color.cyan;
            Handles.DrawLine(position, position + normal * (CursorSize * 2f));
        }
    }

    /// <summary>
    /// Draw UI overlay with instructions
    /// </summary>
    private void DrawUIOverlay()
    {
        Handles.BeginGUI();

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeBackgroundTexture(2, 2, new Color(0f, 0f, 0f, 0.8f));
        boxStyle.padding = new RectOffset(10, 10, 5, 5);

        GUIStyle textStyle = new GUIStyle(EditorStyles.whiteLargeLabel);
        textStyle.fontSize = 12;
        textStyle.normal.textColor = Color.white;
        textStyle.fontStyle = FontStyle.Bold;

        GUILayout.BeginArea(new Rect(10, 10, 280, 80));
        GUILayout.BeginVertical(boxStyle);

        GUILayout.Label("🎨 PAINT MODE ACTIVE", textStyle);

        textStyle.fontSize = 11;
        textStyle.fontStyle = FontStyle.Normal;
        GUILayout.Label("Shift + Click: Create point", textStyle);
        GUILayout.Label("Shift + Drag: Paint continuously", textStyle);

        GUILayout.EndVertical();
        GUILayout.EndArea();

        Handles.EndGUI();
    }

    /// <summary>
    /// Draw settings GUI in Inspector/Window
    /// </summary>
    public void DrawSettingsGUI()
    {
        EditorGUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.HelpBox(
            "Press P in Scene to toggle Paint Mode.\n" +
            "Shift + Click on surfaces to create spawn points.\n" +
            "Shift + Hold to paint continuously.",
            MessageType.Info
        );
        EditorGUILayout.BeginHorizontal();

        // Foldout for paint mode options
        showPaintModeOptions = EditorGUILayout.Foldout(showPaintModeOptions, " Paint Mode", true, EditorStyles.foldoutHeader);
        //GUILayout.Label(" Paint Mode", EditorStyles.boldLabel, GUILayout.Width(150));
        IsActive = EditorGUILayout.Toggle(IsActive, GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();

        if (showPaintModeOptions)
        {
            EditorGUI.indentLevel++;
            PointSpacing = EditorGUILayout.Slider("Point Spacing", PointSpacing, 0.1f, 10f);
            CursorSize = EditorGUILayout.Slider("Cursor Size", CursorSize, 0.1f, 5f);
            AlignToNormal = EditorGUILayout.Toggle("Align to Surface", AlignToNormal);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Surface Detection:", EditorStyles.boldLabel);

            SurfaceLayerMask = LayerMaskField("Paint on Layers", SurfaceLayerMask);
            RaycastDistance = EditorGUILayout.Slider("Raycast Distance", RaycastDistance, 100f, 5000f);

            EditorGUILayout.Space(5);
            // EditorGUILayout.LabelField("Naming:", EditorStyles.boldLabel);
            PointNamePrefix = EditorGUILayout.TextField("Point Name Prefix", PointNamePrefix);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual:", EditorStyles.boldLabel);
            ShowCursor = EditorGUILayout.Toggle("Show Cursor", ShowCursor);
            if (ShowCursor)
            {
                CursorColor = EditorGUILayout.ColorField("Cursor Color", CursorColor);
            }

            EditorGUI.indentLevel--;

            GUI.enabled = true;

            EditorGUILayout.Space(5);

            // Reset counter button
            if (GUILayout.Button("Reset Point Counter"))
            {
                pointCounter = 0;
                Debug.Log("[SpawnPointPainter] Point counter reset");
            }

        }


        if (EditorGUI.EndChangeCheck() && IsActive)
        {
            SceneView.lastActiveSceneView?.ShowNotification(
                new GUIContent("🎨 Paint Mode Activated!\nShift + Click to paint spawn points"),
                2f
            );
        }

        if (!IsActive)
        {
            GUI.enabled = false;
        }


    }

    /// <summary>
    /// Reset painter state
    /// </summary>
    public void Reset()
    {
        isPainting = false;
        pointCounter = 0;
        lastPaintPosition = Vector3.zero;
    }

    /// <summary>
    /// Helper method for LayerMask field
    /// </summary>
    private LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        var tempMask = UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask);
        tempMask = EditorGUILayout.MaskField(label, tempMask, UnityEditorInternal.InternalEditorUtility.layers);
        return UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
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