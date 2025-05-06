using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor.ShaderGraph.Internal;
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

    [Header("New Wall Props")]
    [SerializeField] private string wallNewPrefabPath = "MeshNewWall_Base";
    [SerializeField] private float spaceWallNew = 12.5f;
    [SerializeField] private float minZWallNew = -43.75f;
    [SerializeField] private float maxZWallNew = 43.75f;
    [SerializeField] private GameObject wallNewContainer;

    [Header("New Floor Props")]
    [SerializeField] private string floorNewPrefabPath = "MeshNewFloor_Base";
    [SerializeField] private float spaceZFloorNew = 12.5f;
    [SerializeField] private float minZFloornew = -43.75f;
    [SerializeField] private float maxZFloorNew = 43.75f;

    [SerializeField] private float spaceXFloorNew = 5f;
    [SerializeField] private float minXFloorNew = -8f;
    [SerializeField] private float maxXFloorNew = 8f;
    [SerializeField] private GameObject floorNewContainer;

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

    [Button("Init Wall New ")]
    private void InitNewWall()
    {
        wallCount = Mathf.RoundToInt((maxZWallNew - minZWallNew) / spaceWallNew) + 1;
        Debug.Log($"Wall Count: {wallCount}");
        for (int i = 0; i < wallCount; i++)
        {
            GameObject wall = Instantiate(Resources.Load(wallNewPrefabPath) as GameObject);
            wall.transform.SetParent(wallNewContainer.transform);

            wall.transform.localPosition = new Vector3(0, -1, minZWallNew + (i * spaceWallNew));
            wall.transform.localEulerAngles = new Vector3(0, 0, 0);
            if (wallMesh.Length == 0)
            {
                Debug.LogError("Wall Mesh is empty!");
                continue;
            }
        }

    }

    [Button("Clear WallNew")]
    private void ClearWallNew()
    {
        for (int i = wallNewContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(wallNewContainer.transform.GetChild(i).gameObject);
        }
    }

    float randomUpDown;
    [Button("Init Floor New")]
    private void InitNewFloor()
    {
        wallCount = Mathf.RoundToInt((maxZFloorNew - minZFloornew) / spaceZFloorNew) + 1;
        Debug.Log($"Wall Count: {wallCount}");
        for (int i = 0; i < wallCount; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                GameObject wall = Instantiate(Resources.Load(floorNewPrefabPath) as GameObject);
                wall.transform.SetParent(floorNewContainer.transform);

                randomUpDown = Random.Range(-0.1f, 0.1f);
                Debug.Log($"Random Up Down: {randomUpDown}");

                wall.transform.localPosition = new Vector3(minXFloorNew + (j * spaceXFloorNew) - spaceXFloorNew, -1 + randomUpDown, minZFloornew + (i * spaceZFloorNew));
                wall.transform.localEulerAngles = new Vector3(0, 0, 0);
                if (wallMesh.Length == 0)
                {
                    Debug.LogError("Wall Mesh is empty!");
                    continue;
                }
            }

        }

    }

    [Button("Clear Floor New")]
    private void ClearFloorNew()
    {
        for (int i = floorNewContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(floorNewContainer.transform.GetChild(i).gameObject);
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
