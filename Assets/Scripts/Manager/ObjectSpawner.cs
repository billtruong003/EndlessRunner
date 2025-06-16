using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Prefabs")]
    [SerializeField] private List<GameObject> obstaclePrefabs;
    [SerializeField] private List<GameObject> collectiblePrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private float laneDistance = 3f; // Should match PlayerController
    [SerializeField] private float spawnSpacing = 10f; // Spacing between each spawn pattern along Z-axis
    [SerializeField] private float spawnAheadDistance = 100f; // How far ahead of the player to spawn
    [SerializeField] private float despawnBehindDistance = 20f; // How far behind the player to despawn
    [SerializeField][Range(0, 1)] private float collectibleSpawnChance = 0.5f; // Chance for a collectible in a safe lane

    private float nextSpawnZ;
    private List<GameObject> spawnedObjectContainers = new List<GameObject>();

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player transform not assigned in ObjectSpawner!");
            this.enabled = false;
            return;
        }

        // Start spawning from the player's initial position
        nextSpawnZ = player.position.z;
    }

    void Update()
    {
        if (player == null) return;

        // Spawn new objects ahead of the player
        while (nextSpawnZ < player.position.z + spawnAheadDistance)
        {
            SpawnPattern(new Vector3(0, 0, nextSpawnZ));
            nextSpawnZ += spawnSpacing;
        }

        // Despawn objects far behind the player
        DespawnOldObjects();
    }

    void SpawnPattern(Vector3 position)
    {
        if (obstaclePrefabs.Count == 0 && collectiblePrefabs.Count == 0) return;

        // Create a container for this row of objects to make cleanup easy
        GameObject patternContainer = new GameObject($"Pattern_{position.z}");
        patternContainer.transform.position = position;
        patternContainer.transform.SetParent(this.transform);
        spawnedObjectContainers.Add(patternContainer);

        List<int> availableLanes = new List<int> { 0, 1, 2 }; // 0: Left, 1: Middle, 2: Right
        int obstaclesToSpawn = 0;

        // Decide how many obstacles to spawn (0, 1, or 2)
        if (obstaclePrefabs.Count > 0)
        {
            obstaclesToSpawn = Random.Range(0, 3); // Ensures at least one safe lane
        }

        List<int> obstacleLanes = new List<int>();

        // Randomly pick lanes for obstacles
        for (int i = 0; i < obstaclesToSpawn; i++)
        {
            int laneIndex = Random.Range(0, availableLanes.Count);
            obstacleLanes.Add(availableLanes[laneIndex]);
            availableLanes.RemoveAt(laneIndex);
        }

        // Spawn objects in their designated lanes
        for (int i = 0; i < 3; i++) // Iterate through all 3 lanes
        {
            float xPos = (i - 1) * laneDistance;
            Vector3 spawnPos = new Vector3(xPos, position.y, position.z);

            if (obstacleLanes.Contains(i))
            {
                // Spawn an obstacle in this lane
                if (obstaclePrefabs.Count > 0)
                {
                    GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
                    Instantiate(prefab, spawnPos, prefab.transform.rotation, patternContainer.transform);
                }
            }
            else
            {
                // This is a safe lane, potentially spawn a collectible
                if (collectiblePrefabs.Count > 0 && Random.value < collectibleSpawnChance)
                {
                    GameObject prefab = collectiblePrefabs[Random.Range(0, collectiblePrefabs.Count)];
                    Instantiate(prefab, spawnPos, prefab.transform.rotation, patternContainer.transform);
                }
            }
        }
    }

    void DespawnOldObjects()
    {
        // Use a for loop to safely remove from the list while iterating
        for (int i = spawnedObjectContainers.Count - 1; i >= 0; i--)
        {
            GameObject container = spawnedObjectContainers[i];
            if (container.transform.position.z < player.position.z - despawnBehindDistance)
            {
                spawnedObjectContainers.RemoveAt(i);
                Destroy(container);
            }
        }
    }

    public void ResetSpawner()
    {
        // Clear all existing spawned objects
        for (int i = spawnedObjectContainers.Count - 1; i >= 0; i--)
        {
            Destroy(spawnedObjectContainers[i]);
        }
        spawnedObjectContainers.Clear();

        // Reset spawn position
        if (player != null)
        {
            nextSpawnZ = player.position.z;
        }
    }
}