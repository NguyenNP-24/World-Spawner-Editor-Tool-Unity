using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace WorldSpawnerTool
{
    /// <summary>
    /// Static UI helper class - All UI drawing methods, no logic
    /// </summary>
    public static class WorldSpawnerUI
    {
        /// <summary>
        /// Draw Paint Mode settings UI
        /// </summary>
        public static bool DrawPaintModeUI(PaintSettings settings, bool foldout)
        {
            EditorGUILayout.BeginHorizontal();
            string statusText = settings.isActive ? "Active" : "Inactive";

            // Define colors for active/inactive status
            Color statusColor = settings.isActive ? new Color(0.1f, 0.7f, 0.1f, 1f) : new Color(0.7f, 0.1f, 0.1f, 1f);

            GUIContent foldoutContent = new GUIContent($" Paint Mode (<color=#{ColorUtility.ToHtmlStringRGBA(statusColor)}>{statusText}</color>)");

            // Color richText enabled
            foldout = EditorGUILayout.Foldout(
                foldout,
                foldoutContent,
                true,
                new GUIStyle(EditorStyles.foldoutHeader) { richText = true } 
            );

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            bool newState = EditorGUILayout.Toggle(settings.isActive, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                settings.isActive = newState;

                string message = settings.isActive ? " Paint Mode ON" : "Paint Mode OFF";
                SceneView.lastActiveSceneView?.ShowNotification(new GUIContent(message), 2f);
            }

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(
                    "Press P in Scene View to toggle Paint Mode.\n" +
                    "Shift + Click/Drag on surfaces to create spawn points.\n Shift + Drag to paint continuously." +
                    "Ctrl+Shift + Click/Drag to erase spawn points.",
                    MessageType.Info
                );

                // Minimum distance between points when dragging
                settings.pointSpacing = EditorGUILayout.Slider(
                new GUIContent("Point Spacing", "Minimum distance between points when dragging"),
                settings.pointSpacing, 0.1f, 10f
                );

                // Cursor size display - visual only purpose
                settings.cursorSize = EditorGUILayout.Slider(
                new GUIContent("Cursor Size", "Cursor size display - visual only purpose"),
                settings.cursorSize, 0.1f, 5f);

                // Align to surface normal, y axis points up
                settings.alignToNormal = EditorGUILayout.Toggle(
                    new GUIContent("Align to Surface", "Align new points to surface normal (Y axis points up)"),
                    settings.alignToNormal
                );

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Surface Detection:", EditorStyles.boldLabel);

                // Layer mask of surfaces to hit
                settings.surfaceLayerMask = LayerMaskField(
                    new GUIContent("Paint on Layers", "Layers considered as valid surfaces"),
                    settings.surfaceLayerMask
                );

                EditorGUILayout.Space(3);

                // Point naming
                settings.pointNamePrefix = EditorGUILayout.TextField(
                    new GUIContent("Point Name Prefix", "Prefix for naming new spawn points"),
                    settings.pointNamePrefix
                );

                EditorGUILayout.Space(3);

                if (settings.showCursor)
                {
                    // Cursor color customization
                    settings.cursorColor = EditorGUILayout.ColorField(new GUIContent("Paint Cursor Color", "Color of the paint cursor"), settings.cursorColor);
                    settings.eraseCursorColor = EditorGUILayout.ColorField(new GUIContent("Erase Cursor Color", "Color of the erase cursor"), settings.eraseCursorColor);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            return foldout;
        }

        /// <summary>
        /// Draw Spawn Points list UI
        /// </summary>
        public static bool DrawSpawnPointsListUI(
            SpawnPointManager manager,
            bool foldout,
            System.Action onAddEmpty,
            System.Action onAddSelected,
            System.Action onClearAll)
        {
            foldout = EditorGUILayout.Foldout(
                foldout,
                $" Spawn Points ({manager.Count})",
                true,
                EditorStyles.foldoutHeader
            );

            if (foldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox("Add spawn points via PaintMode, manually or select objects from scene.", MessageType.Info);

                // Action buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Empty"))
                {
                    onAddEmpty?.Invoke();
                }

                if (GUILayout.Button("Add Selected"))
                {
                    onAddSelected?.Invoke();
                }

                if (GUILayout.Button("Clear All"))
                {
                    if (EditorUtility.DisplayDialog("Clear All", "Remove all spawn points?", "Yes", "No"))
                    {
                        onClearAll?.Invoke();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // Display list
                if (manager.Count == 0)
                {
                    EditorGUILayout.HelpBox("No spawn points added yet.", MessageType.Warning);
                }
                else
                {
                    DrawSpawnPointsList(manager);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            return foldout;
        }

        /// <summary>
        /// Draw individual spawn points in the list
        /// </summary>
        private static void DrawSpawnPointsList(SpawnPointManager manager)
        {
            int removeIndex = -1;

            for (int i = 0; i < manager.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                Transform newPoint = (Transform)EditorGUILayout.ObjectField(
                    $"Point {i + 1}",
                    manager.Get(i),
                    typeof(Transform),
                    true
                );

                // Update if changed
                if (newPoint != manager.Get(i))
                {
                    manager.Set(i, newPoint);
                }

                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Remove after loop to avoid GUI errors
            if (removeIndex >= 0)
            {
                manager.Remove(removeIndex);
                GUI.FocusControl(null);
            }
        }

        /// <summary>
        /// Draw Spawn Options UI
        /// </summary>
        public static bool DrawSpawnOptionsUI(SpawnSettings settings, bool foldout)
        {
            foldout = EditorGUILayout.Foldout(
                foldout,
                " Spawn Options",
                true,
                EditorStyles.foldoutHeader
            );

            if (foldout)
            {
                EditorGUI.indentLevel++;

                // Prefix for naming new Spawn objects, empty = use prefab name
                settings.objectNamePrefix = EditorGUILayout.TextField(new GUIContent("Object Name Prefix",
                "Prefix for naming spawned objects, empty = use prefab name"), settings.objectNamePrefix
                );

                // Align to Surface
                settings.alignToSurface = EditorGUILayout.Toggle(new GUIContent("Align to Surface",
                "Align spawned objects to surface normal (Y axis points up)"), settings.alignToSurface
                );

                if (settings.alignToSurface)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox("Green line = hit, Red line = no hit.", MessageType.Info);

                    // Layer mask of surfaces to hit
                    settings.surfaceLayerMask = LayerMaskField(
                    new GUIContent("Surface Layers", "Layers considered as valid surfaces"),
                    settings.surfaceLayerMask
                    );

                    // Max raycast distance to detect surface
                    settings.raycastDistanceSpawn = EditorGUILayout.Slider(
                        new GUIContent("Raycast Distance", "Max raycast distance to detect surface"),
                        settings.raycastDistanceSpawn, 5f, 100f
                    );
                    EditorGUI.indentLevel--;
                }

                // Random Rotation
                settings.useRandomRotation = EditorGUILayout.Toggle(new GUIContent("Random Rotation",
                "Apply random rotation on selected axes"), settings.useRandomRotation
                );
                if (settings.useRandomRotation)
                {
                    EditorGUI.indentLevel++;
                    if (settings.alignToSurface)
                    {
                        EditorGUILayout.HelpBox("Random rotation applied AFTER surface alignment.", MessageType.Info);
                    }

                    EditorGUILayout.BeginHorizontal();
                    settings.randomRotationX = EditorGUILayout.ToggleLeft("X Axis", settings.randomRotationX, GUILayout.Width(90));
                    settings.randomRotationY = EditorGUILayout.ToggleLeft("Y Axis", settings.randomRotationY, GUILayout.Width(90));
                    settings.randomRotationZ = EditorGUILayout.ToggleLeft("Z Axis", settings.randomRotationZ, GUILayout.Width(90));
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }

                // Random Scale
                settings.useRandomScale = EditorGUILayout.Toggle(new GUIContent("Random Scale",
                "Apply uniform random scale within specified range"), settings.useRandomScale
                );
                if (settings.useRandomScale)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Scale Range:", GUILayout.Width(EditorGUIUtility.labelWidth - 4));

                    int prevIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    EditorGUILayout.LabelField("Min:", GUILayout.Width(30));
                    settings.scaleRange.x = EditorGUILayout.FloatField(settings.scaleRange.x, GUILayout.Width(50));

                    GUILayout.Label("-", GUILayout.Width(15));

                    EditorGUILayout.LabelField("Max:", GUILayout.Width(32));
                    settings.scaleRange.y = EditorGUILayout.FloatField(settings.scaleRange.y, GUILayout.Width(50));

                    EditorGUI.indentLevel = prevIndent;
                    EditorGUILayout.EndHorizontal();
                }

                settings.parentToSpawnPoint = EditorGUILayout.Toggle(new GUIContent("Parent to Point",
                "Make spawned objects children of their respective spawn points"), settings.parentToSpawnPoint
                );
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            return foldout;
        }


        /// <summary>
        /// Draw Preview Options UI
        /// </summary>
        public static bool DrawPreviewOptionsUI(PreviewSettings settings, bool foldout)
        {
            EditorGUILayout.BeginHorizontal();

            // Foldout (left side)
            foldout = EditorGUILayout.Foldout(
                foldout,
                " Preview Options",
                true,
                EditorStyles.foldoutHeader
            );

            GUILayout.FlexibleSpace();

            settings.showPreview = EditorGUILayout.Toggle(settings.showPreview, GUILayout.Width(20));

            EditorGUILayout.EndHorizontal();

            if (foldout && settings.showPreview)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                settings.showNames = EditorGUILayout.Toggle("Show Names", settings.showNames);

                GUI.enabled = settings.showNames;
                settings.showNameBackground = EditorGUILayout.Toggle("Text Background", settings.showNameBackground);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                settings.showDirections = EditorGUILayout.Toggle("Show Directions", settings.showDirections);
                settings.previewColor = EditorGUILayout.ColorField("Sphere Color", settings.previewColor);
                settings.previewSphereSize = EditorGUILayout.Slider("Sphere Size", settings.previewSphereSize, 0.1f, 10f);

                if (settings.showDirections)
                {
                    settings.previewArrowSize = EditorGUILayout.Slider("Arrow Size", settings.previewArrowSize, 0.5f, 5f);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            return foldout;
        }

        /// <summary>
        /// Helper method for LayerMask field
        /// </summary>
        private static LayerMask LayerMaskField(GUIContent label, LayerMask layerMask)
        {
            var tempMask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask);
            tempMask = EditorGUILayout.MaskField(label, tempMask, InternalEditorUtility.layers);
            return InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
        }
    }
}