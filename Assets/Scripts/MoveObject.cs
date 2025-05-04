using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MoveObject : MonoBehaviour
{
    private enum LerpState
    {
        PingPongLerp,
        MoveLerp,
    }

    [SerializeField] private LerpState lerpState;
    [SerializeField] private Vector3 rootPosition; // 1
    [SerializeField] private Transform target; // 2
    [SerializeField] private int indexPos;

    private Vector3 moveStep;
    float distanceFromTarget;

    void Start()
    {
        indexPos = 1;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchLerpState();
        }
    }

    private void SwitchLerpState()
    {
        if (lerpState != LerpState.PingPongLerp)
        {
            lerpState = LerpState.PingPongLerp;
            return;
        }
        lerpState = LerpState.MoveLerp;
    }

    private void MoveLogic(Vector3 targetPose)
    {
        Move(targetPose);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
            transform.position = rootPosition;
    }

    private void PingPongLerp()
    {
        switch (indexPos)
        {
            case 1:
                distanceFromTarget = Vector3.Distance(transform.position, rootPosition);
                if (distanceFromTarget > 0.5f)
                {
                    Move(rootPosition);
                    transform.position = moveStep;
                    return;
                }
                indexPos = 2;
                break;
            case 2:
                distanceFromTarget = Vector3.Distance(transform.position, target.position);
                if (distanceFromTarget > 0.5f)
                {
                    Move(target.position);
                    return;
                }
                indexPos = 1;
                break;
        }
    }

    private void Move(Vector3 target)
    {
        moveStep = VectorLerp(transform.position, target, Time.deltaTime);
        transform.position = moveStep;
    }

    private Vector3 VectorLerp(Vector3 firstPose, Vector3 targetPose, float timestamp)
    {
        return Vector3.Lerp(firstPose, targetPose, timestamp);
    }

    void FixedUpdate()
    {
        switch (lerpState)
        {
            case LerpState.MoveLerp:
                MoveLogic(target.position);
                break;
            case LerpState.PingPongLerp:
                PingPongLerp();
                break;
        }
    }
}
