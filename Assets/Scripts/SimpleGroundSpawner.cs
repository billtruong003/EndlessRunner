using System.Collections.Generic;
using UnityEngine;

public class SimpleGroundSpawner : MonoBehaviour
{
    [Header("Ground Settings")]
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private float groundLength = 100f;
    [SerializeField] private float groundSpacing = 5f; // Space between ground pieces
    [SerializeField] private int groundAhead = 3; // Number of grounds to keep ahead
    [SerializeField] private int groundBehind = 1; // Number of grounds to keep behind

    private Queue<Transform> groundQueue = new Queue<Transform>();
    private float nextSpawnZ = 0f;
    private float totalGroundDistance; // Length + spacing
    private float lastPlayerZ = 0f; // Track last player position

    void Start()
    {
        if (!groundPrefab || !player)
        {
            Debug.LogError("SimpleGroundSpawner: Missing references!");
            return;
        }

        // Calculate total distance for each ground piece (length of ground plus the gap to the next)
        totalGroundDistance = groundLength + groundSpacing;

        // Initialize spawn position based on player's starting position
        // Assuming ground origin is at the start, position it so player starts on ground
        nextSpawnZ = player.position.z - (groundLength / 2f); // Center the first ground under player if needed
        nextSpawnZ -= (groundBehind * totalGroundDistance); // Adjust for behind pieces (now in positive Z direction, so subtract)

        // Spawn initial grounds (from behind to ahead in positive direction)
        for (int i = 0; i < groundBehind + groundAhead + 1; i++)
        {
            SpawnGround(nextSpawnZ);
            nextSpawnZ += totalGroundDistance; // Move to next position in positive direction
        }

        // Set initial player position
        if (player != null)
        {
            lastPlayerZ = player.position.z;
        }
    }

    void Update()
    {
        if (!player) return;

        float playerZ = player.position.z;

        // Only process if player moved forward (positive Z direction for endless runner)
        if (playerZ > lastPlayerZ)
        {
            lastPlayerZ = playerZ;

            // Calculate the furthest ground position we need (in positive direction)
            float furthestNeededZ = playerZ + (groundAhead * totalGroundDistance);

            // Spawn new grounds if needed
            while (nextSpawnZ < furthestNeededZ) // Check if we need to spawn more ground ahead
            {
                SpawnGround(nextSpawnZ);
                nextSpawnZ += totalGroundDistance; // Move spawn position in positive direction
            }

            // Remove grounds that are too far behind (in negative direction)
            while (groundQueue.Count > 0)
            {
                Transform oldestGround = groundQueue.Peek();
                if (oldestGround != null && oldestGround.position.z < playerZ - (groundBehind * totalGroundDistance))
                {
                    groundQueue.Dequeue();
                    Destroy(oldestGround.gameObject);
                }
                else
                {
                    break; // This ground is still needed
                }
            }
        }
    }

    void SpawnGround(float zPosition)
    {
        GameObject newGround = Instantiate(groundPrefab, new Vector3(0, 0, zPosition), Quaternion.identity, transform);
        groundQueue.Enqueue(newGround.transform);

        Debug.Log($"Spawned ground at Z: {zPosition}");
    }


    public void ResetSpawner()
    {
        // Clear all existing grounds
        while (groundQueue.Count > 0)
        {
            Transform ground = groundQueue.Dequeue();
            if (ground != null)
            {
                Destroy(ground.gameObject);
            }
        }

        // Reset variables
        nextSpawnZ = player.position.z - (groundLength / 2f); // Align with player position
        nextSpawnZ -= (groundBehind * totalGroundDistance); // Adjust for behind pieces in positive Z direction
        lastPlayerZ = 0f;

        // Respawn initial grounds
        for (int i = 0; i < groundBehind + groundAhead + 1; i++)
        {
            SpawnGround(nextSpawnZ);
            nextSpawnZ += totalGroundDistance; // Move in positive Z direction
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw ground pieces
        Gizmos.color = Color.green;
        foreach (Transform ground in groundQueue)
        {
            if (ground != null)
            {
                Vector3 center = ground.position + Vector3.up * 0.5f;
                Vector3 size = new Vector3(10f, 1f, groundLength);
                Gizmos.DrawWireCube(center, size);
            }
        }

        // Draw next spawn position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(0, 1, nextSpawnZ), 2f);

        // Draw player position indicator
        if (player != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector3(0, 2, player.position.z), 1f);
        }

        // Draw spawn boundaries
        if (player != null)
        {
            float playerZ = player.position.z;
            Gizmos.color = Color.red;
            // Behind boundary (positive direction)
            Gizmos.DrawLine(new Vector3(-5, 0, playerZ + groundBehind * totalGroundDistance),
                           new Vector3(5, 0, playerZ + groundBehind * totalGroundDistance));
            // Ahead boundary (negative direction)
            Gizmos.DrawLine(new Vector3(-5, 0, playerZ - groundAhead * totalGroundDistance),
                           new Vector3(5, 0, playerZ - groundAhead * totalGroundDistance));
        }
    }
}