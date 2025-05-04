using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public enum PropsType
{
    Wall
}

public class PropsSetup : MonoBehaviour
{
    [Header("Floor Props")]
    [SerializeField] private string floorPrefabPath = "Floor";
    [SerializeField] private float spaceFloor = 5f;
    [SerializeField] private float minXFloor = -10f;
    [SerializeField] private float maxXFloor = 10f;
    [SerializeField] private float minZFloor = -50f;
    [SerializeField] private float maxZFloor = 50f;
    [SerializeField] private GameObject floorContainer;
    [SerializeField] private Mesh[] floorMesh;

    [Header("Wall Props")]
    [SerializeField] private string wallPrefabPath = "MeshWall_Base";
    [SerializeField] private float spaceWall = 5f;
    [SerializeField] private float minZWall = -50f;
    [SerializeField] private float maxZWall = 50f;
    [SerializeField] private GameObject wallLeftContainer;
    [SerializeField] private GameObject wallRightContainer;
    [SerializeField] private Mesh[] wallMesh;

    
    private int wallCount = 0;
    [Button("Clear All")]
    private void ClearAll()
    {
        ClearWall();
    }

    [Button("Init Wall")]
    private void InitWall()
    {
        wallCount = Mathf.FloorToInt((maxZWall - minZWall) / spaceWall);
        Debug.Log($"Wall Count: {wallCount}");
        for (int i = 0; i < wallCount; i++)
        {
            GameObject wall = Instantiate(Resources.Load(wallPrefabPath) as GameObject);
            wall.transform.SetParent(wallLeftContainer.transform);
            wall.transform.localPosition = new Vector3(0, 0, minZWall + (i * spaceWall));
            wall.transform.localEulerAngles = new Vector3(90, 90, 0);
            if (wallMesh.Length == 0)
            {
                Debug.LogError("Wall Mesh is empty!");
                continue;
            }
            MeshFilter meshFilter = wall.GetComponent<MeshFilter>();
            meshFilter.mesh = wallMesh[Random.Range(0, wallMesh.Length)];
        }
        for (int i = 0; i < wallCount; i++)
        {
            GameObject wall = Instantiate(Resources.Load(wallPrefabPath) as GameObject);
            wall.transform.SetParent(wallRightContainer.transform);
            wall.transform.localPosition = new Vector3(0, 0, minZWall + (i * spaceWall));
            wall.transform.localEulerAngles = new Vector3(90, -90, 0);
            wall.transform.localScale = new Vector3(-1 * wall.transform.localScale.x, wall.transform.localScale.y, wall.transform.localScale.z);

            if (wallMesh.Length == 0)
            {
                Debug.LogError("Wall Mesh is empty!");
                continue;
            }
            MeshFilter meshFilter = wall.GetComponent<MeshFilter>();
            meshFilter.mesh = wallMesh[Random.Range(0, wallMesh.Length)];
        }

    }

    [Button("Clear Wall")]
    private void ClearWall()
    {
        for (int i = wallLeftContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(wallLeftContainer.transform.GetChild(i).gameObject);
        }
        for (int i = wallRightContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(wallRightContainer.transform.GetChild(i).gameObject);
        }
    }

    MeshFilter floorFilter;
    [Button("InitFloor")]
    private void InitFloor()
    {
        int floorCount = Mathf.FloorToInt((maxZFloor - minZFloor) / spaceFloor);
        Debug.Log($"Floor Count: {floorCount}");
        for (int i = 0; i < floorCount + 4; i++)
        {
            int flootAtX = Mathf.FloorToInt((maxXFloor - minXFloor) / spaceFloor);
            Debug.Log($"Floor At X: {flootAtX}");
            for (int j = 0; j < flootAtX; j++)
            {
                GameObject floorX = Instantiate(Resources.Load(floorPrefabPath) as GameObject);
                floorX.transform.SetParent(floorContainer.transform);
                floorX.transform.localPosition = new Vector3(minXFloor + spaceFloor + (j * spaceFloor), 0, minZFloor + (i * spaceFloor));
                MeshFilter floorFilter = floorX.GetComponent<MeshFilter>();
                floorFilter.mesh = floorMesh[Random.Range(0, floorMesh.Length)];
                floorX.transform.localScale = new Vector3(1, 1, 1);
            }
        }
    }

    [Button("Clear Floor")]
    private void ClearFloor()
    {
        for (int i = floorContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(floorContainer.transform.GetChild(i).gameObject);
        }
    }

}
