using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WorldSpawnerTool
{
    /// <summary>
    /// Manages the list of spawn points - Pure logic, no UI
    /// </summary>
    public class SpawnPointManager
    {
        private List<Transform> spawnPoints = new List<Transform>();

        // Properties
        public int Count => spawnPoints.Count;
        public IReadOnlyList<Transform> Points => spawnPoints.AsReadOnly();

        /// <summary>
        /// Add a single spawn point
        /// </summary>
        public void Add(Transform point)
        {
            if (point != null && !spawnPoints.Contains(point))
            {
                spawnPoints.Add(point);
            }
        }

        /// <summary>
        /// Add multiple spawn points (sorted by name)
        /// </summary>
        public int AddRange(IEnumerable<Transform> points)
        {
            int addedCount = 0;
            var sortedPoints = points.OrderBy(t => t.name).ToList();

            foreach (Transform point in sortedPoints)
            {
                if (point != null && !spawnPoints.Contains(point))
                {
                    spawnPoints.Add(point);
                    addedCount++;
                }
            }

            return addedCount;
        }

        /// <summary>
        /// Add an empty (null) placeholder point
        /// </summary>
        public void AddEmpty()
        {
            spawnPoints.Add(null);
        }

        /// <summary>
        /// Get spawn point at index
        /// </summary>
        public Transform Get(int index)
        {
            if (index >= 0 && index < spawnPoints.Count)
                return spawnPoints[index];
            return null;
        }

        /// <summary>
        /// Set spawn point at index
        /// </summary>
        public void Set(int index, Transform point)
        {
            if (index >= 0 && index < spawnPoints.Count)
            {
                spawnPoints[index] = point;
            }
        }

        /// <summary>
        /// Remove spawn point at index
        /// </summary>
        public void Remove(int index)
        {
            if (index >= 0 && index < spawnPoints.Count)
            {
                spawnPoints.RemoveAt(index);
            }
        }

        /// <summary>
        /// Clear all spawn points
        /// </summary>
        public void Clear()
        {
            spawnPoints.Clear();
        }

        /// <summary>
        /// Get count of valid (non-null) spawn points
        /// </summary>
        public int GetValidCount()
        {
            return spawnPoints.Count(p => p != null);
        }

        /// <summary>
        /// Remove all null points from the list
        /// </summary>
        public int RemoveNullPoints()
        {
            int removedCount = spawnPoints.RemoveAll(p => p == null);
            return removedCount;
        }

        /// <summary>
        /// Check if a point exists in the list
        /// </summary>
        public bool Contains(Transform point)
        {
            return spawnPoints.Contains(point);
        }

        /// <summary>
        /// Get index of a spawn point
        /// </summary>
        public int IndexOf(Transform point)
        {
            return spawnPoints.IndexOf(point);
        }

        /// <summary>
        /// Build a dictionary mapping spawn points to their 1-based indices
        /// </summary>
        public Dictionary<Transform, int> BuildIndicesDictionary()
        {
            var dict = new Dictionary<Transform, int>();
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null)
                {
                    dict[spawnPoints[i]] = i + 1; // 1-based index
                }
            }
            return dict;
        }
    }
}