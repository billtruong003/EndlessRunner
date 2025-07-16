// FileName: LevelChunkSpawner.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public sealed class LevelChunkSpawner : MonoBehaviour
{
    public event Action<Transform> OnChunkSpawned;

    [Header("Core Dependencies")]
    [Tooltip("The player's transform to track for spawning and recycling chunks.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Reference to the GameManager to control game state transitions.")]
    [SerializeField] private GameManager gameManager;

    [Header("Chunk Configuration")]
    [Tooltip("List of chunk prefabs to be spawned randomly.")]
    [SerializeField] private List<GameObject> chunkPrefabs;
    [Tooltip("The length of each individual chunk along the Z-axis.")]
    [SerializeField] private float chunkLength = 100f;

    [Header("Spawning Window")]
    [Tooltip("How many chunks should be maintained in front of the player.")]
    [SerializeField] private int chunksToMaintainAhead = 3;
    [Tooltip("How many chunks should be maintained behind the player.")]
    [SerializeField] private int chunksToMaintainBehind = 1;

    [SerializeField] private float nextSpawnPositionZ;
    [SerializeField] private int lastSpawnedPrefabIndex = -1;

    private readonly LinkedList<GameObject> activeChunks = new LinkedList<GameObject>();
    private Dictionary<string, Queue<GameObject>> chunkObjectPools;

    private void Start()
    {
        ValidateConfiguration();
        InitializeObjectPools();
        SubscribeToGameEvents();
        ResetSpawnerState();
    }

    private void Update()
    {
        if (CanUpdateSpawner())
        {
            ManageChunkSpawning();
            ManageChunkRecycling();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    private void ValidateConfiguration()
    {
        if (playerTransform == null)
            Debug.LogError("Player Transform is not assigned.", this);
        if (gameManager == null)
            Debug.LogError("Game Manager is not assigned.", this);
        if (chunkPrefabs == null || chunkPrefabs.Count == 0 || chunkPrefabs.Any(p => p == null))
            Debug.LogError("Chunk Prefabs list is not configured correctly.", this);
        if (chunkLength <= 0)
            Debug.LogError("Chunk Length must be a positive value.", this);
    }

    private void InitializeObjectPools()
    {
        chunkObjectPools = new Dictionary<string, Queue<GameObject>>();
        int totalChunksInWindow = chunksToMaintainAhead + chunksToMaintainBehind + 1;
        int poolBufferSize = 2;
        int poolSizePerPrefab = totalChunksInWindow + poolBufferSize;

        foreach (var prefab in chunkPrefabs)
        {
            var objectQueue = new Queue<GameObject>();
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject chunkInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity, this.transform);
                chunkInstance.name = prefab.name;
                chunkInstance.SetActive(false);
                objectQueue.Enqueue(chunkInstance);
            }
            chunkObjectPools[prefab.name] = objectQueue;
        }
    }

    private void SubscribeToGameEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStart += ResetSpawnerState;
        }
    }

    private void UnsubscribeFromGameEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStart -= ResetSpawnerState;
        }
    }

    private bool CanUpdateSpawner()
    {
        return playerTransform != null && gameManager != null;
    }

    private void ManageChunkSpawning()
    {
        float spawnBoundaryZ = playerTransform.position.z + (chunksToMaintainAhead * chunkLength);

        while (nextSpawnPositionZ < spawnBoundaryZ)
        {
            SpawnNextChunk();
        }
    }

    private void ManageChunkRecycling()
    {
        if (activeChunks.Count == 0) return;

        float recycleBoundaryZ = playerTransform.position.z - (chunksToMaintainBehind * chunkLength);
        GameObject oldestChunk = activeChunks.First.Value;
        float oldestChunkBackEdgeZ = oldestChunk.transform.position.z + (chunkLength / 2f);

        if (oldestChunkBackEdgeZ < recycleBoundaryZ)
        {
            RecycleChunk(oldestChunk);
        }
    }

    private void SpawnNextChunk()
    {
        GameObject selectedPrefab = SelectRandomNonRepeatingPrefab();
        if (selectedPrefab == null) return;

        Queue<GameObject> poolQueue = chunkObjectPools[selectedPrefab.name];
        GameObject chunkToSpawn = poolQueue.Count > 0
            ? poolQueue.Dequeue()
            : InstantiateNewChunkForPool(selectedPrefab);

        PositionAndActivateChunk(chunkToSpawn);
    }

    private GameObject SelectRandomNonRepeatingPrefab()
    {
        if (chunkPrefabs.Count == 1) return chunkPrefabs[0];

        int nextPrefabIndex;
        do
        {
            nextPrefabIndex = UnityEngine.Random.Range(0, chunkPrefabs.Count);
        } while (nextPrefabIndex == lastSpawnedPrefabIndex);

        lastSpawnedPrefabIndex = nextPrefabIndex;
        return chunkPrefabs[nextPrefabIndex];
    }

    private GameObject InstantiateNewChunkForPool(GameObject prefab)
    {
        Debug.LogWarning($"Object pool for '{prefab.name}' was empty. Instantiating a new one. Consider increasing the pool buffer size.");
        GameObject newChunkInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity, this.transform);
        newChunkInstance.name = prefab.name;
        return newChunkInstance;
    }

    private void PositionAndActivateChunk(GameObject chunk)
    {
        chunk.transform.position = new Vector3(0, 0, nextSpawnPositionZ);
        chunk.SetActive(true);
        activeChunks.AddLast(chunk);
        nextSpawnPositionZ += chunkLength;

        var chunkIdentifier = chunk.GetComponent<ChunkIdentifier>();
        if (chunkIdentifier != null && chunkIdentifier.PatternSpawnOrigin != null)
        {
            OnChunkSpawned?.Invoke(chunkIdentifier.PatternSpawnOrigin);
        }
        else
        {
            Debug.LogWarning($"Chunk '{chunk.name}' is missing a properly configured 'ChunkIdentifier' component. No patterns will be placed on it.", chunk);
        }
    }

    private void RecycleChunk(GameObject chunk)
    {
        activeChunks.RemoveFirst();
        chunk.SetActive(false);

        if (chunkObjectPools.TryGetValue(chunk.name, out Queue<GameObject> poolQueue))
        {
            poolQueue.Enqueue(chunk);
        }
        else
        {
            Debug.LogWarning($"Could not find an object pool for chunk '{chunk.name}' to recycle. Destroying instead.");
            Destroy(chunk);
        }
    }

    public void ResetSpawnerState()
    {
        while (activeChunks.Count > 0)
        {
            RecycleChunk(activeChunks.First.Value);
        }

        lastSpawnedPrefabIndex = -1;

        float playerStartGridZ = (playerTransform != null)
            ? Mathf.Floor(playerTransform.position.z / chunkLength) * chunkLength
            : 0f;

        float firstChunkZ = playerStartGridZ - (chunksToMaintainBehind * chunkLength);
        nextSpawnPositionZ = firstChunkZ;

        int totalChunksToSpawn = chunksToMaintainAhead + chunksToMaintainBehind + 1;
        for (int i = 0; i < totalChunksToSpawn; i++)
        {
            SpawnNextChunk();
        }
    }
}