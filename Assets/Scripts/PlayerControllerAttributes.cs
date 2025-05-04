using UnityEngine;
using System;

public enum PlayerPoint
{
    None,
    Left,
    Right,
    Center,
}

[Serializable]
public class StandPoint
{
    public PlayerPoint playerPoint = PlayerPoint.None;
    public Transform pointTransform;
    public Vector3 position;

    public StandPoint(Transform pointTransform)
    {
        this.pointTransform = pointTransform;
        position = pointTransform.position;
    }

    public void Init()
    {
        if (pointTransform == null)
        {
            Debug.LogError("Point transform is not assigned.");
            return;
        }
        
        pointTransform.position = position;
    }

    public void SetStandPointPosition(Vector3 position)
    {
        pointTransform.position = position;
    }

    public Vector3 GetPositionPoint()
    {
        return pointTransform.position;
    }
}
