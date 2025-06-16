using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance = 50f; // Distance ahead to spawn
    [SerializeField] private float minSpawnInterval = 20f;
    [SerializeField] private float maxSpawnInterval = 40f;
    [SerializeField] private float despawnDistance = 10f; // Distance behind player to despawn

    [Header("Lane Settings")]
    [SerializeField] private float laneDistance = 3f;
    [SerializeField] private float[] laneXPositions = { -3f, 0f, 3f }; // Left, Center, Right

    [Header("Obstacle Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private GameObject[] collectablePrefabs;

    [Header("Spawn Patterns")]
    [SerializeField] private ObstaclePattern[] obstaclePatterns;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform obstacleContainer;

    private float lastSpawnZ = 0f;
    private float nextSpawnDistance;
    private List<GameObject> activeObstacles = new List<GameObject>();
    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
        nextSpawnDistance = Random.Range(minSpawnInterval, maxSpawnInterval);

        // Spawn initial obstacles
        for (int i = 0; i < 3; i++)
        {
            SpawnObstacleSet(20f + i * 30f);
        }
    }

    private void Update()
    {
        if (!gameManager.IsPlaying()) return;

        // Check if we need to spawn new obstacles
        if (player.position.z > lastSpawnZ - spawnDistance + nextSpawnDistance)
        {
            SpawnObstacleSet(lastSpawnZ + nextSpawnDistance);

            // Adjust spawn interval based on difficulty
            float difficultyMultiplier = gameManager.GetDifficultyMultiplier();
            float adjustedMin = Mathf.Max(10f, minSpawnInterval / difficultyMultiplier);
            float adjustedMax = Mathf.Max(15f, maxSpawnInterval / difficultyMultiplier);
            nextSpawnDistance = Random.Range(adjustedMin, adjustedMax);
        }

        // Despawn obstacles that are too far behind
        DespawnOldObstacles();
    }

    private void SpawnObstacleSet(float zPosition)
    {
        lastSpawnZ = zPosition;

        // Choose a random pattern or spawn individual obstacles
        if (obstaclePatterns.Length > 0 && Random.Range(0f, 1f) < 0.6f)
        {
            SpawnPattern(zPosition);
        }
        else
        {
            SpawnRandomObstacles(zPosition);
        }

        // Chance to spawn collectables
        if (Random.Range(0f, 1f) < 0.7f)
        {
            SpawnCollectables(zPosition + Random.Range(5f, 15f));
        }
    }

    private void SpawnPattern(float zPosition)
    {
        ObstaclePattern pattern = obstaclePatterns[Random.Range(0, obstaclePatterns.Length)];

        foreach (var spawn in pattern.spawnPoints)
        {
            if (spawn.obstacleType != null)
            {
                Vector3 spawnPos = new Vector3(
                    laneXPositions[spawn.lane],
                    spawn.height,
                    zPosition + spawn.zOffset
                );

                GameObject obstacle = Instantiate(spawn.obstacleType, spawnPos, Quaternion.identity, obstacleContainer);
                activeObstacles.Add(obstacle);
            }
        }
    }

    private void SpawnRandomObstacles(float zPosition)
    {
        // Randomly choose lanes to block (but always leave at least one open)
        List<int> availableLanes = new List<int> { 0, 1, 2 };
        int lanesToBlock = Random.Range(1, 3); // Block 1 or 2 lanes

        for (int i = 0; i < lanesToBlock; i++)
        {
            int laneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
            availableLanes.Remove(laneIndex);

            if (obstaclePrefabs.Length > 0)
            {
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                Vector3 spawnPos = new Vector3(laneXPositions[laneIndex], 0f, zPosition);

                GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleContainer);
                activeObstacles.Add(obstacle);
            }
        }
    }

    private void SpawnCollectables(float zPosition)
    {
        if (collectablePrefabs.Length == 0) return;

        // Choose a lane for collectables
        int lane = Random.Range(0, 3);
        int collectableCount = Random.Range(3, 8);

        GameObject collectablePrefab = collectablePrefabs[Random.Range(0, collectablePrefabs.Length)];

        for (int i = 0; i < collectableCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                laneXPositions[lane],
                1f, // Height for collectables
                zPosition + i * 2f
            );

            GameObject collectable = Instantiate(collectablePrefab, spawnPos, Quaternion.identity, obstacleContainer);
            activeObstacles.Add(collectable);
        }
    }

    private void DespawnOldObstacles()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            if (activeObstacles[i].transform.position.z < player.position.z - despawnDistance)
            {
                Destroy(activeObstacles[i]);
                activeObstacles.RemoveAt(i);
            }
        }
    }
}

[System.Serializable]
public class ObstaclePattern
{
    public string patternName;
    public ObstacleSpawnPoint[] spawnPoints;
}

[System.Serializable]
public class ObstacleSpawnPoint
{
    public GameObject obstacleType;
    public int lane; // 0 = left, 1 = center, 2 = right
    public float zOffset; // Offset from the base spawn position
    public float height = 0f; // Y position
}