using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;
using System;

public class Floor : MonoBehaviour
{
    [Header("Floor Props")]
    [SerializeField] private string FloorProps = "Floor";
    [SerializeField] private Vector2 point1 = new Vector2(-4f, -4f); // Điểm 1 (x,z)
    [SerializeField] private Vector2 point2 = new Vector2(4f, 4f);   // Điểm 2 (x,z)
    [SerializeField] private float spacing = 4f; // Khoảng cách giữa các props
    [SerializeField] private Material material;
    [SerializeField] private List<FloorProps> propList = new List<FloorProps>();

    [Button("Floor Spawn")]
    public void InitFloor()
    {
        for (int x = (int)point1.x; x <= (int)point2.x; x += (int)spacing)
        {
            for (int z = (int)point1.y; z <= (int)point2.y; z += (int)spacing)
            {
                FloorProps floorProps = GetRandomPropByPercentage();
                GameObject prefabFloor = floorProps.Prefab;
                if (prefabFloor != null)
                {
                    GameObject newProp = Instantiate(prefabFloor, new Vector3(x, 0, z), Quaternion.identity, transform);
                    newProp.transform.eulerAngles = new Vector3(-90, 0, 0);
                    newProp.transform.SetParent(transform);
                    newProp.gameObject.name = floorProps.Name + "_" + x + "_" + z;

                    // Kiểm tra xem prefab có MeshRenderer không
                    MeshRenderer meshRenderer = newProp.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        // Nếu có, gán material
                        meshRenderer.material = material;
                    }
                    else
                    {
                        Debug.LogWarning($"Prefab {prefabFloor.name} does not have a MeshRenderer component.");
                    }
                }
            }
        }
    }

    [Button("Clear All")]
    public void ClearAll()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    public FloorProps GetRandomPropByPercentage()
    {
        // Tính tổng tất cả Percentage
        int totalPercentage = 0;
        foreach (var prop in propList)
        {
            totalPercentage += prop.Percentage;
        }

        // Nếu tổng phần trăm bằng 0, trả về null để tránh lỗi
        if (totalPercentage == 0)
        {
            Debug.LogWarning("Total percentage is 0. No props can be selected.");
            return null;
        }

        // Tạo một số ngẫu nhiên từ 0 đến totalPercentage
        int randomValue = UnityEngine.Random.Range(0, totalPercentage);

        // Duyệt qua danh sách propList để chọn prop dựa trên phần trăm
        int cumulativePercentage = 0;
        foreach (var prop in propList)
        {
            cumulativePercentage += prop.Percentage;
            if (randomValue < cumulativePercentage)
            {
                return prop; // Trả về Prefab của prop được chọn
            }
        }

        // Nếu không chọn được prop nào (trường hợp lỗi), trả về null
        Debug.LogWarning("No prop selected. Check the percentage values.");
        return null;
    }
}

[Serializable]
public class FloorProps
{
    public string Name;
    public int Id;
    public int Percentage;
    public GameObject Prefab;
    public FloorPropsType Type;

    public FloorProps(string name, int id)
    {
        this.Name = name;
        this.Id = id;
    }
}

public enum FloorPropsType
{
    FloorDefault,
    FloorBroken,
    FloorBroken2,
    FloorGrass,
    FloorGrass2,
    FloorGrassDirt,
}