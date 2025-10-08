using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace WorldSpawnerTool
{
    /// <summary>
    /// Result data from a spawn operation
    /// </summary>
    public class SpawnResult
    {
        public int SpawnedCount;
        public int SkippedCount;
        public List<GameObject> SpawnedObjects = new List<GameObject>();

        public string GetMessage()
        {
            string message = $"Successfully spawned {SpawnedCount} object(s)!";
            if (SkippedCount > 0)
                message += $"\nSkipped {SkippedCount} empty spawn point(s).";
            return message;
        }
    }

    /// <summary>
    /// Pure logic for spawning objects - No UI dependencies
    /// </summary>
    public static class ObjectSpawner
    {
        /// <summary>
        /// Spawn objects at all spawn points with given settings
        /// </summary>
        public static SpawnResult SpawnObjects(
            GameObject prefab,
            IReadOnlyList<Transform> spawnPoints,
            SpawnSettings settings)
        {
            if (prefab == null)
            {
                Debug.LogError("[ObjectSpawner] Prefab is null!");
                return null;
            }

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogError("[ObjectSpawner] No spawn points provided!");
                return null;
            }

            var result = new SpawnResult();
            string finalPrefix = settings.GetFinalPrefix(prefab);

            // Build point indices for naming
            var pointIndices = BuildPointIndices(spawnPoints);

            // Group into single Undo operation
            Undo.SetCurrentGroupName("Spawn Objects");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                {
                    result.SkippedCount++;
                    continue;
                }

                GameObject spawnedObject = InstantiateObject(prefab, spawnPoint, settings, pointIndices, finalPrefix);

                if (spawnedObject != null)
                {
                    result.SpawnedObjects.Add(spawnedObject);
                    result.SpawnedCount++;

                    // Register for undo
                    Undo.RegisterCreatedObjectUndo(spawnedObject, "Spawn Objects");
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"[ObjectSpawner] {result.GetMessage()}");
            return result;
        }

        /// <summary>
        /// Instantiate a single object at a spawn point
        /// </summary>
        private static GameObject InstantiateObject(
            GameObject prefab,
            Transform spawnPoint,
            SpawnSettings settings,
            Dictionary<Transform, int> pointIndices,
            string namePrefix)
        {
            // Instantiate
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            // Position
            obj.transform.position = spawnPoint.position;

            // Rotation
            Quaternion finalRotation = CalculateRotation(spawnPoint, settings);
            obj.transform.rotation = finalRotation;

            // Scale
            if (settings.useRandomScale)
            {
                float randomScale = Random.Range(settings.scaleRange.x, settings.scaleRange.y);
                obj.transform.localScale = Vector3.one * randomScale;
            }

            // Parent
            if (settings.parentToSpawnPoint)
            {
                obj.transform.SetParent(spawnPoint);
            }

            // Name
            int pointIndex = pointIndices.ContainsKey(spawnPoint) ? pointIndices[spawnPoint] : 0;
            obj.name = namePrefix + pointIndex;

            return obj;
        }

        /// <summary>
        /// Calculate final rotation based on settings
        /// </summary>
        private static Quaternion CalculateRotation(Transform spawnPoint, SpawnSettings settings)
        {
            Quaternion finalRotation = spawnPoint.rotation;

            // Step 1: Align to surface if enabled
            if (settings.alignToSurface)
            {
                RaycastHit hit;
                Vector3 rayStart = spawnPoint.position + Vector3.up * 10f;
                float rayLength = settings.raycastDistanceSpawn + 10f;

                if (Physics.Raycast(rayStart, Vector3.down, out hit, rayLength, settings.surfaceLayerMask))
                {
                    Vector3 forward = Vector3.ProjectOnPlane(spawnPoint.forward, hit.normal);
                    finalRotation = Quaternion.LookRotation(forward, hit.normal);
                }
            }

            // Step 2: Apply random rotation
            if (settings.useRandomRotation)
            {
                Vector3 randomEuler = finalRotation.eulerAngles;
                if (settings.randomRotationX) randomEuler.x = Random.Range(0f, 360f);
                if (settings.randomRotationY) randomEuler.y = Random.Range(0f, 360f);
                if (settings.randomRotationZ) randomEuler.z = Random.Range(0f, 360f);
                finalRotation = Quaternion.Euler(randomEuler);
            }

            return finalRotation;
        }

        /// <summary>
        /// Build dictionary mapping spawn points to 1-based indices
        /// </summary>
        private static Dictionary<Transform, int> BuildPointIndices(IReadOnlyList<Transform> spawnPoints)
        {
            var dict = new Dictionary<Transform, int>();
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null)
                {
                    dict[spawnPoints[i]] = i + 1; // 1-based
                }
            }
            return dict;
        }

        /// <summary>
        /// Delete all objects in the list
        /// </summary>
        public static int DeleteObjects(List<GameObject> objects)
        {
            // Clean up null references
            objects.RemoveAll(obj => obj == null);

            if (objects.Count == 0)
                return 0;

            // Group into single Undo operation
            Undo.SetCurrentGroupName("Delete Spawned Objects");
            int undoGroup = Undo.GetCurrentGroup();

            int deletedCount = 0;
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                if (objects[i] != null)
                {
                    Undo.DestroyObjectImmediate(objects[i]);
                    deletedCount++;
                }
            }

            objects.Clear();
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"[ObjectSpawner] Deleted {deletedCount} objects");
            return deletedCount;
        }

        /// <summary>
        /// Delete spawn point GameObjects from scene
        /// </summary>
        public static int DeleteSpawnPoints(IReadOnlyList<Transform> spawnPoints, PaintSettings paintSettings)
        {
            if (spawnPoints == null && paintSettings == null)
                return 0;

            Undo.SetCurrentGroupName("Delete Spawn Points");
            int undoGroup = Undo.GetCurrentGroup();

            int deletedCount = 0;

            // Delete provided spawn points
            if (spawnPoints != null)
            {
                for (int i = spawnPoints.Count - 1; i >= 0; i--)
                {
                    if (spawnPoints[i] != null)
                    {
                        Undo.DestroyObjectImmediate(spawnPoints[i].gameObject);
                        deletedCount++;
                    }
                }
            }

            // Also delete by name prefix in case some points were not tracked
            if (!string.IsNullOrEmpty(paintSettings.pointNamePrefix))
            {
                var allObjects = UnityEngine.Object.FindObjectsOfType<Transform>(true);
                foreach (var obj in allObjects)
                {
                    if (obj.name.StartsWith(paintSettings.pointNamePrefix, System.StringComparison.Ordinal))
                    {
                        Undo.DestroyObjectImmediate(obj.gameObject);
                        deletedCount++;
                    }
                }

                // mark scene dirty
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }


            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());


            Debug.Log($"[ObjectSpawner] Deleted {deletedCount} spawn points");
            return deletedCount;
        }

    }
}