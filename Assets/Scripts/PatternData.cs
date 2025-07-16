// FileName: PatternData.cs (Phiên bản nâng cấp với NaughtyAttributes)

using UnityEngine;
using System.Collections.Generic;
using EndlessRunner.Definitions;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "NewPattern", menuName = "Endless Runner/Pattern Data")]
public class PatternData : ScriptableObject
{
    // Lớp SpawnableObjectDefinition không đổi
    [System.Serializable]
    public class SpawnableObjectDefinition
    {
        public string objectName;
        public SpawnableType type;
        public string poolTag;
        public LanePosition lane;
        public float zOffset;
        public float yOffset = 0.5f;
    }

    [Header("Pattern Info")]
    public string patternName = "Default Pattern";

    [Tooltip("Độ dài của pattern này trên trục Z. Việc sinh tự động sẽ tuân theo giá trị này.")]
    public float patternLength = 20f;

    [Tooltip("Độ khó của pattern, dùng để điều khiển khi nào nó xuất hiện.")]
    [Range(0, 100)]
    public int difficulty = 1;

    // ----- [NEW] CÔNG CỤ TẠO PATTERN TỰ ĐỘNG -----
    [BoxGroup("Pattern Generator")]
    [Tooltip("Chọn một mẫu pattern có sẵn để sinh tự động.")]
    [Dropdown("GetPatternPresetNames")] // Sử dụng Dropdown để có tên đẹp hơn
    public PatternPresetType presetToGenerate;

    [BoxGroup("Generator Parameters")]
    [Tooltip("Khoảng cách giữa các vật phẩm trên trục Z.")]
    public float objectSpacing = 2f;
    [BoxGroup("Generator Parameters")]
    [Tooltip("Pool Tag mặc định cho Coin.")]
    public string coinPoolTag = "Coin";
    [BoxGroup("Generator Parameters")]
    [Tooltip("Pool Tag mặc định cho chướng ngại vật thấp.")]
    public string lowObstaclePoolTag = "ObstacleLow";
    [BoxGroup("Generator Parameters")]
    [Tooltip("Pool Tag mặc định cho chướng ngại vật cao.")]
    public string highObstaclePoolTag = "ObstacleHigh";

    [Button("Generate Pattern", EButtonEnableMode.Editor)]

    // [BoxGroup("Pattern Generator")]
    private void GeneratePatternFromPreset()
    {
        if (presetToGenerate == PatternPresetType.None) return;

        objectsToSpawn.Clear(); // Xóa pattern cũ trước khi tạo mới

        switch (presetToGenerate)
        {
            case PatternPresetType.CoinLine_Straight_Middle:
                for (float z = 0; z < patternLength; z += objectSpacing)
                {
                    CreateSpawnable("Coin", SpawnableType.Coin, coinPoolTag, LanePosition.Middle, z, 1f);
                }
                break;

            case PatternPresetType.CoinLine_Full_Three_Lanes:
                for (float z = 0; z < patternLength; z += objectSpacing)
                {
                    CreateSpawnable("Coin L", SpawnableType.Coin, coinPoolTag, LanePosition.Left, z, 1f);
                    CreateSpawnable("Coin M", SpawnableType.Coin, coinPoolTag, LanePosition.Middle, z, 1f);
                    CreateSpawnable("Coin R", SpawnableType.Coin, coinPoolTag, LanePosition.Right, z, 1f);
                }
                break;

            case PatternPresetType.CoinArch_Middle:
                for (float z = 0; z < patternLength; z += objectSpacing)
                {
                    float normalizedZ = z / patternLength; // 0 to 1
                    float y = 1f + Mathf.Sin(normalizedZ * Mathf.PI) * 3f; // Tạo hình vòng cung
                    CreateSpawnable("Coin Arch", SpawnableType.Coin, coinPoolTag, LanePosition.Middle, z, y);
                }
                break;

            case PatternPresetType.ObstacleWall_Open_Middle:
                CreateSpawnable("Wall L", SpawnableType.Obstacle_High, highObstaclePoolTag, LanePosition.Left, 2f, 1.5f);
                CreateSpawnable("Wall R", SpawnableType.Obstacle_High, highObstaclePoolTag, LanePosition.Right, 2f, 1.5f);
                break;

            case PatternPresetType.ObstacleHurdles_Middle:
                for (float z = 2; z < patternLength; z += objectSpacing * 2)
                {
                    CreateSpawnable("Hurdle", SpawnableType.Obstacle_Low, lowObstaclePoolTag, LanePosition.Middle, z, 0.5f);
                }
                break;

            case PatternPresetType.JumpAndDuck:
                CreateSpawnable("Hurdle", SpawnableType.Obstacle_Low, lowObstaclePoolTag, LanePosition.Middle, 2f, 0.5f);
                CreateSpawnable("High Obstacle", SpawnableType.Obstacle_High, highObstaclePoolTag, LanePosition.Middle, 2f + objectSpacing * 2, 1.5f);
                break;
        }

        // Tự động đánh dấu rằng object đã bị thay đổi để Unity biết và lưu lại
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private DropdownList<PatternPresetType> GetPatternPresetNames()
    {
        return new DropdownList<PatternPresetType>()
        {
            { "--- CHỌN MỘT MẪU ---", PatternPresetType.None },
            { "Coin/Đường Thẳng (Giữa)", PatternPresetType.CoinLine_Straight_Middle },
            { "Coin/Đường Thẳng (3 Làn)", PatternPresetType.CoinLine_Full_Three_Lanes },
            { "Coin/Vòng Cung (Giữa)", PatternPresetType.CoinArch_Middle },
            { "Obstacle/Tường (Mở Giữa)", PatternPresetType.ObstacleWall_Open_Middle },
            { "Obstacle/Vượt Rào (Giữa)", PatternPresetType.ObstacleHurdles_Middle },
            { "Obstacle/Nhảy và Trượt", PatternPresetType.JumpAndDuck },
        };
    }

    private void CreateSpawnable(string name, SpawnableType type, string tag, LanePosition lane, float z, float y)
    {
        objectsToSpawn.Add(new SpawnableObjectDefinition
        {
            objectName = name,
            type = type,
            poolTag = tag,
            lane = lane,
            zOffset = z,
            yOffset = y
        });
    }

    // ----- Danh sách các đối tượng sẽ được sinh ra (kết quả) -----
    [Header("Objects to Spawn")]
    [ReorderableList]
    public List<SpawnableObjectDefinition> objectsToSpawn;
}

// FileName: PatternPresets.cs

public enum PatternPresetType
{
    // --- Basic Coin Patterns ---
    None,
    CoinLine_Straight_Middle,
    CoinLine_Straight_Left,
    CoinLine_Straight_Right,
    CoinLine_Full_Three_Lanes,
    CoinArch_Middle,
    CoinZigZag_LeftToRight,
    CoinSineWave_Middle,

    // --- Basic Obstacle Patterns ---
    Obstacle_Single_Low_Middle, // Requires Jump
    Obstacle_Single_High_Middle, // Requires Slide
    ObstacleWall_Open_Left,
    ObstacleWall_Open_Middle,
    ObstacleWall_Open_Right,
    ObstacleWall_Full_Block, // Forcing a power-up use
    ObstacleHurdles_Middle, // A series of low obstacles
    ObstacleTunnel_Middle, // A series of high obstacles
    ObstaclePillars_Sides, // Blocks left and right lanes

    // --- Mixed Patterns ---
    JumpAndDuck, // Low obstacle followed by a high one
    Path_Weaving, // Series of walls with alternating openings
    PowerUp_Before_Wall,
    CoinTrail_To_PowerUp
}