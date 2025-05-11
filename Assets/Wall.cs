using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;
using System;

public class Wall : MonoBehaviour
{
    [Header("Wall Props")]
    [SerializeField] private string WallProps = "Wall";
    [SerializeField] private MeshFilter wallMesh1; // MeshFilter chứa mesh thứ nhất
    [SerializeField] private MeshFilter wallMesh2; // MeshFilter chứa mesh thứ hai
    [SerializeField] private List<WallProps> propList = new List<WallProps>();

    WallProps wallPropsTemp;
    [Button("Wall Spawn")]
    public void InitWall()
    {

        wallPropsTemp = GetRandomPropByPercentage();
        if (wallPropsTemp != null)
        {
            wallMesh1.mesh = wallPropsTemp.Mesh;
            wallMesh1.GetComponent<MeshRenderer>().material = wallPropsTemp.Material;

        }
        wallPropsTemp = GetRandomPropByPercentage();
        if (wallPropsTemp != null)
        {
            wallMesh2.mesh = wallPropsTemp.Mesh;
            wallMesh2.GetComponent<MeshRenderer>().material = wallPropsTemp.Material;
        }
    }

    [Button("Clear All")]
    public void ClearAll()
    {
        wallMesh1.mesh = null;
        wallMesh2.mesh = null;
    }

    public WallProps GetRandomPropByPercentage()
    {
        int totalPercentage = 0;
        foreach (var prop in propList)
        {
            totalPercentage += prop.Percentage;
        }

        if (totalPercentage == 0)
        {
            Debug.LogWarning("Total percentage is 0. No props can be selected.");
            return null;
        }

        int randomValue = UnityEngine.Random.Range(0, totalPercentage);

        int cumulativePercentage = 0;
        foreach (var prop in propList)
        {
            cumulativePercentage += prop.Percentage;
            if (randomValue < cumulativePercentage)
            {
                return prop;
            }
        }

        Debug.LogWarning("No prop selected. Check the percentage values.");
        return null;
    }
}

[Serializable]
public class WallProps
{
    public string Name;
    public int Id;
    public int Percentage;
    public Mesh Mesh;
    public Material Material; // Material tùy chọn
    public WallPropsType Type; // Type để hỗ trợ ánh xạ mesh (nếu prop.Mesh là null)

    public WallProps(string name, int id)
    {
        this.Name = name;
        this.Id = id;
    }
}

public enum WallPropsType
{
    WallDefault, // Liên kết với wallMesh1 nếu prop.Mesh là null
    WallCracked,   // Liên kết với wallMesh2 nếu prop.Mesh là null
    WallBroken,
    WallArched,
    WallArchedWindowGate,
    WallGateWindowOpen,
}