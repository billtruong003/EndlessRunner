using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;
    [SerializeField] private string animParam = "Centroid";
    [SerializeField] private float moveTime = 0.25f;
    [SerializeField] private PlayerPoint currentPlayerPoint = PlayerPoint.None;
    [SerializeField] private StandPoint[] standPoints;
    [SerializeField] private Animator anim;


    private void Update()
    {
        CharacterMovement();
        
    }

    private void CharacterMovement()
    {
        if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ProcessStandPoint(KeyCode.LeftArrow);
        }
        else if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            ProcessStandPoint(KeyCode.RightArrow);
        }
        else if(Input.GetKeyDown(KeyCode.Space))
        {
            ProcessStandPoint(KeyCode.UpArrow);
        }
    }

    private void ProcessStandPoint(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.LeftArrow:
                ProcessMovePoint(PlayerPoint.Left);
                break;
            case KeyCode.RightArrow:
                ProcessMovePoint(PlayerPoint.Right);
                break;
            case KeyCode.UpArrow:
            //jump
                break;
            default:
                break;
        }
    }

    PlayerPoint playerPoint = PlayerPoint.None;
    private void ProcessMovePoint(PlayerPoint point)
    {   
        bool isLeft = point == PlayerPoint.Left;
        playerPoint = validPlayerPoint(isLeft);

        Debug.Log($"Player Point: {playerPoint}");
        if (currentPlayerPoint == playerPoint)
            return;

        SetAnimBlend(playerPoint);
        if (currentPlayerPoint == PlayerPoint.Center)
        {
            MoveToStandPoint(playerPoint);
            currentPlayerPoint = playerPoint;
        }
        else if (currentPlayerPoint == PlayerPoint.Right || currentPlayerPoint == PlayerPoint.Left)
        {
            MoveToStandPoint(PlayerPoint.Center);
            currentPlayerPoint = PlayerPoint.Center;
        }
    }

    private PlayerPoint validPlayerPoint(bool isLeft)
    {
        if (isLeft)
            return PlayerPoint.Left;
        else
            return PlayerPoint.Right;
    }
    
    private void SetAnimBlend(PlayerPoint point)
    {
        switch (point)
        {
            case PlayerPoint.Left:
                TweenAnimation(-1f);
                break;
            case PlayerPoint.Right:
                TweenAnimation(1f);
                break;
            default:
                break;
        }
        DOVirtual.DelayedCall(moveTime * 2, () => TweenAnimation(0f), true);
    }

    private void TweenAnimation(float value)
    {
        DOTween.To(() => anim.GetFloat(animParam), x => anim.SetFloat(animParam, x), value, moveTime).SetEase(Ease.OutFlash);
    }
    private void MoveToStandPoint(PlayerPoint point)
    {
        foreach (var standPoint in standPoints)
        {
            if (standPoint.playerPoint == point)
            {
                transform.DOMove(standPoint.GetPositionPoint(), moveTime).SetEase(Ease.OutFlash);
                cameraController.MoveToTarget(standPoint.GetPositionPoint(), moveTime);
                break;
            }
        }
    }

    
    [Button("Set Stand Point")]
    private void InitStandPoints()
    {
        foreach (var standPoint in standPoints)
        {
            standPoint.Init();
        }
    }
}
