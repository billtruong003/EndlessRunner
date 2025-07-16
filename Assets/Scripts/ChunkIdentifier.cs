// FileName: ChunkIdentifier.cs

using UnityEngine;

public class ChunkIdentifier : MonoBehaviour
{
    [Tooltip("Điểm gốc để đặt các pattern. Thường là một Transform con ở đầu chunk.")]
    [SerializeField] private Transform patternSpawnOrigin;

    public Transform PatternSpawnOrigin => patternSpawnOrigin;
}