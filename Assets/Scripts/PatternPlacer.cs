// FileName: PatternPlacer.cs

using UnityEngine;

public class PatternPlacer : MonoBehaviour
{
    [Header("Core Dependencies")]
    [SerializeField] private LevelChunkSpawner chunkSpawner;
    [SerializeField] private PatternDatabase patternDatabase;
    [SerializeField] private PlayerStat playerStat;

    [Header("Difficulty Configuration")]
    [Tooltip("Độ khó tối đa ban đầu khi game mới bắt đầu.")]
    [SerializeField] private int initialDifficulty = 5;
    [Tooltip("Sau mỗi khoảng thời gian này (giây), độ khó tối đa sẽ tăng lên.")]
    [SerializeField] private float difficultyIncreaseInterval = 30f;

    [Header("Spawning Configuration")]
    [Tooltip("Chiều rộng của một làn đường, dùng để tính toán vị trí X.")]
    [SerializeField] private float laneWidth = 3f;

    private ObjectPooler pooler;
    private float lastZPositionPlaced = 0f;
    private float gameTime = 0f;

    private void Start()
    {
        ValidateDependencies();
        pooler = ObjectPooler.Instance;
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying())
        {
            gameTime += Time.deltaTime;
        }
    }

    private void ValidateDependencies()
    {
        if (chunkSpawner == null)
            Debug.LogError("Chunk Spawner is not assigned in PatternPlacer.", this);
        if (patternDatabase == null)
            Debug.LogError("Pattern Database is not assigned in PatternPlacer. Please assign the ScriptableObject.", this);
        if (playerStat == null)
            Debug.LogError("Player Stat is not assigned in PatternPlacer.", this);
    }

    private void SubscribeToEvents()
    {
        if (chunkSpawner != null)
        {
            chunkSpawner.OnChunkSpawned += HandleChunkSpawned;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (chunkSpawner != null)
        {
            chunkSpawner.OnChunkSpawned -= HandleChunkSpawned;
        }
    }

    private void HandleChunkSpawned(Transform chunkSpawnOrigin)
    {
        // Đảm bảo không đặt pattern lên cùng một chunk nhiều lần
        if (chunkSpawnOrigin.position.z <= lastZPositionPlaced) return;

        PlacePatternsOnChunk(chunkSpawnOrigin);
    }

    private void PlacePatternsOnChunk(Transform spawnOrigin)
    {
        int currentMaxDifficulty = CalculateCurrentMaxDifficulty();
        PatternData selectedPattern = patternDatabase.GetRandomPattern(currentMaxDifficulty);

        if (selectedPattern == null)
        {
            Debug.LogWarning($"No suitable pattern found for max difficulty '{currentMaxDifficulty}'. Skipping pattern placement for this chunk. Check if your PatternDatabase contains patterns with difficulty <= {currentMaxDifficulty}.");
            return;
        }

        foreach (var itemDefinition in selectedPattern.objectsToSpawn)
        {
            SpawnObjectFromDefinition(itemDefinition, spawnOrigin);
        }

        lastZPositionPlaced = spawnOrigin.position.z;
    }

    private void SpawnObjectFromDefinition(PatternData.SpawnableObjectDefinition definition, Transform origin)
    {
        float xPosition = (int)definition.lane * laneWidth;
        Vector3 spawnPosition = origin.position + new Vector3(xPosition, definition.yOffset, definition.zOffset);

        pooler.SpawnFromPool(definition.poolTag, spawnPosition, Quaternion.identity);
    }

    private int CalculateCurrentMaxDifficulty()
    {
        int timeBasedDifficulty = (int)(gameTime / difficultyIncreaseInterval);
        return initialDifficulty + timeBasedDifficulty;
    }

    public void ResetPlacer()
    {
        lastZPositionPlaced = -1f; // Reset về giá trị âm để đảm bảo chunk đầu tiên luôn được xử lý
        gameTime = 0f;
    }
}