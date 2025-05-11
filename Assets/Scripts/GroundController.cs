using System.Collections.Generic;
using UnityEngine;

public class GroundController : MonoBehaviour
{
    [Header("Parameters Setting")]
    [SerializeField] private int groundCount = 10;
    [SerializeField] private int groundSize = 5;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float groundLength = 10f;
    [SerializeField] private float groundWidth = 10f;

    [Header("Ground Prefab Setting")]
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private Ground[] grounds;

    private Vector3 positionSpawn;
    private Vector3 positionEnd;
    private float groundSpace;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        groundSpace = groundLength;
        positionSpawn = new Vector3(0, 0, 0);
        positionEnd = new Vector3(0, 0, -groundLength);

        grounds = new Ground[groundCount];
        for (int i = 0; i < groundCount; i++)
        {
            GameObject groundObj = Instantiate(groundPrefab, transform);
            Vector3 spawnPos = new Vector3(0, 0, i * groundSpace);
            Vector3 endPos = new Vector3(0, 0, spawnPos.z - groundLength);

            grounds[i] = new Ground(groundObj.transform);
            grounds[i].SetGroundPosition(spawnPos, endPos);
        }
    }

    void Update()
    {
        foreach (Ground ground in grounds)
        {
            if (ground.groundTransform == null) continue;

            Transform groundTransform = ground.groundTransform;
            groundTransform.position += Vector3.back * speed * Time.deltaTime;

            if (groundTransform.position.z <= positionEnd.z)
            {
                float maxZ = float.MinValue;
                foreach (Ground g in grounds)
                {
                    if (g.groundTransform.position.z > maxZ)
                    {
                        maxZ = g.groundTransform.position.z;
                    }
                }

                Vector3 newPos = new Vector3(0, 0, maxZ + groundLength);
                ground.SetGroundPosition(newPos, new Vector3(0, 0, newPos.z - groundLength));
            }
        }
    }
}

public class Ground
{
    public Transform groundTransform;
    public Vector3 groundPositionSpawn;
    public Vector3 groundPositionEnd;

    public Vector3 GetSpawnPose() => groundPositionSpawn;
    public Vector3 GetEndPose() => groundPositionEnd;

    public Ground(Transform groundTransform)
    {
        this.groundTransform = groundTransform;
    }

    public void SetGroundPosition(Vector3 positionSpawn, Vector3 positionEnd)
    {
        groundTransform.localPosition = positionSpawn;
        groundPositionSpawn = positionSpawn;
        groundPositionEnd = positionEnd;
    }
}