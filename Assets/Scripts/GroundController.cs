using System.Collections;
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
    private int groundIndex = 0;
    private float groundSpace;
    private float multiplierLogic;
    private float startPose;
    private float endPose;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        multiplierLogic = groundLength * groundSize;
        startPose = multiplierLogic - 10;
        endPose = -(multiplierLogic + 10);
        grounds = new Ground[groundCount];
        Ground ground = null;

        for (int i = 0; i < groundCount; i++)
        {
            GameObject groundObject = Instantiate(groundPrefab, this.transform);
            multiplierLogic = groundLength * groundSize * i;

            positionSpawn = new Vector3(0, 0, (multiplierLogic * 2) + startPose);
            positionEnd = new Vector3(0, 0, endPose);

            groundObject.transform.localScale = new Vector3(groundWidth, 1, groundLength);

            ground = new Ground(groundObject.transform);
            ground.SetGroundPosition(positionSpawn, positionEnd);


            grounds[i] = ground;
        }

    }

    Transform groundTransform = null;
    void Update()
    {
        foreach (Ground ground in grounds)
        {
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
                groundTransform.position = new Vector3(0, 0, maxZ + multiplierLogic * 2);
            }
        }

    }
}

public class Ground
{
    public Transform groundTransform;
    public Vector3 groundPositionSpawn;
    public Vector3 groundPositionEnd;

    public Vector3 getSpawnPose() => groundPositionSpawn;
    public Vector3 getEndPose() => groundPositionEnd;

    public Ground(Transform groundTransform)
    {
        this.groundTransform = groundTransform;
    }

    public void SetGroundPosition(Vector3 PositionSpawn, Vector3 PositionEnd)
    {
        groundTransform.position = PositionSpawn;
        groundPositionSpawn = PositionSpawn;
        groundPositionEnd = PositionEnd;
    }



}