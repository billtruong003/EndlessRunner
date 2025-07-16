// FileName: PatternDatabase.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "PatternDatabase", menuName = "Endless Runner/Pattern Database")]
public class PatternDatabase : ScriptableObject
{
    [SerializeField]
    private List<PatternData> allPatterns;

    private Dictionary<int, List<PatternData>> patternsByDifficulty;
    private bool isInitialized = false;

    private void Initialize()
    {
        // Log khi bắt đầu quá trình khởi tạo
        Debug.Log("<color=cyan>DATABASE: Bắt đầu quá trình Initialize()...</color>");

        if (isInitialized)
        {
            Debug.Log("<color=lime>DATABASE: Đã được khởi tạo từ trước. Bỏ qua.</color>");
            return;
        }

        // Kiểm tra xem danh sách gốc có bị null không
        if (allPatterns == null)
        {
            Debug.LogError("<color=red>DATABASE: LỖI NGHIÊM TRỌNG! Danh sách 'allPatterns' bị null. Hãy chắc chắn rằng nó đã được khởi tạo trong script.</color>");
            return; // Dừng lại để tránh lỗi nặng hơn
        }

        patternsByDifficulty = new Dictionary<int, List<PatternData>>();
        Debug.Log($"<color=white>DATABASE: Tìm thấy {allPatterns.Count} pattern(s) trong danh sách 'allPatterns'.</color>");

        foreach (var pattern in allPatterns)
        {
            // Kiểm tra xem có phần tử nào trong danh sách bị null không
            if (pattern == null)
            {
                Debug.LogError("<color=red>DATABASE: LỖI NGHIÊM TRỌNG! Một phần tử trong 'allPatterns' bị 'None' (null). Hãy kiểm tra lại Inspector của PatternDatabase asset.</color>");
                continue; // Bỏ qua phần tử null này và tiếp tục với các phần tử khác
            }

            // Log thông tin của pattern đang được xử lý
            Debug.Log($"<color=yellow>DATABASE: Đang xử lý pattern '{pattern.name}' với độ khó {pattern.difficulty}.</color>");

            if (!patternsByDifficulty.ContainsKey(pattern.difficulty))
            {
                patternsByDifficulty.Add(pattern.difficulty, new List<PatternData>());
            }
            patternsByDifficulty[pattern.difficulty].Add(pattern);
        }

        isInitialized = true;
        Debug.Log($"<color=green>DATABASE: Khởi tạo hoàn tất. Đã phân loại pattern vào {patternsByDifficulty.Count} nhóm độ khó.</color>");
    }

    public PatternData GetRandomPattern(int maxDifficulty)
    {
        Initialize();

        // Kiểm tra xem Dictionary có được khởi tạo thành công không
        if (patternsByDifficulty == null)
        {
            Debug.LogError("<color=red>DATABASE: LỖI NGHIÊM TRỌNG! Dictionary 'patternsByDifficulty' bị null sau khi Initialize(). Quá trình khởi tạo đã thất bại.</color>");
            return null;
        }

        var availableDifficulties = patternsByDifficulty.Keys
            .Where(d => d <= maxDifficulty)
            .ToList();

        if (availableDifficulties.Count == 0)
        {
            // Cảnh báo này rất quan trọng để gỡ lỗi
            Debug.LogWarning($"<color=orange>DATABASE: Không tìm thấy pattern nào có độ khó <= {maxDifficulty}. Trả về null.</color>");
            return null;
        }

        int randomDifficulty = availableDifficulties[Random.Range(0, availableDifficulties.Count)];
        var patterns = patternsByDifficulty[randomDifficulty];
        var selectedPattern = patterns[Random.Range(0, patterns.Count)];

        Debug.Log($"<color=lime>DATABASE: Đã chọn pattern '{selectedPattern.name}' từ nhóm độ khó {randomDifficulty}.</color>");

        return selectedPattern;
    }
}