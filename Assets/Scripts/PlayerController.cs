using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    // Serialized fields for Unity Inspector
    [Header("References")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private string laneBlendParameter = "Centroid";

    [Header("Movement Settings")]
    [SerializeField] private float laneSwitchTime = 0.25f;
    [SerializeField] private PlayerPoint currentLane = PlayerPoint.None;
    [SerializeField] private StandPoint[] lanePoints;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float jumpDuration = 0.5f;

    [Header("Slide Settings")]
    [SerializeField] private float slideHeight = -0.5f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideFromJumpDuration = 0.2f;

    [Header("Camera Settings")]
    [SerializeField] private float cameraFollowSpeed = 0.1f;

    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f; // Minimum distance for a swipe
    [SerializeField] private float maxSwipeTime = 0.5f; // Max time for a swipe

    // Player state management
    private enum PlayerState { Idle, Jumping, Sliding }
    private PlayerState currentState = PlayerState.Idle;
    private Vector3 initialPosition;
    private Tween verticalTween;
    private Tween lateralTween;
    private bool isVerticalInputLocked; // Blocks Up/Down during specific tweens
    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool isTouching;

    private void Start()
    {
        initialPosition = transform.position;
        if (currentLane == PlayerPoint.None)
            currentLane = PlayerPoint.Center;
    }

    private void Update()
    {
        ProcessInput();
    }

    private void LateUpdate()
    {
        cameraController.MoveToTarget(transform.position, cameraFollowSpeed);
    }

    // Handle both keyboard and touch swipe input
    private void ProcessInput()
    {
        // Keyboard input
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ProcessAction(PlayerAction.MoveLeft);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            ProcessAction(PlayerAction.MoveRight);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            ProcessAction(PlayerAction.Jump);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            ProcessAction(PlayerAction.Slide);

        // Touch swipe input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    touchStartTime = Time.time;
                    isTouching = true;
                    break;
                case TouchPhase.Ended:
                    if (isTouching)
                    {
                        Vector2 touchEndPos = touch.position;
                        float touchDuration = Time.time - touchStartTime;
                        if (touchDuration <= maxSwipeTime)
                        {
                            Vector2 swipeDelta = touchEndPos - touchStartPos;
                            if (swipeDelta.magnitude >= minSwipeDistance)
                            {
                                ProcessSwipe(swipeDelta);
                            }
                        }
                        isTouching = false;
                    }
                    break;
            }
        }
    }

    // Process swipe direction
    private void ProcessSwipe(Vector2 swipeDelta)
    {
        float absX = Mathf.Abs(swipeDelta.x);
        float absY = Mathf.Abs(swipeDelta.y);

        if (absX > absY)
        {
            // Horizontal swipe
            Debug.Log($"Swipe horizontal: {(swipeDelta.x > 0 ? "Right" : "Left")}");
            ProcessAction(swipeDelta.x > 0 ? PlayerAction.MoveRight : PlayerAction.MoveLeft);
        }
        else
        {
            // Vertical swipe
            Debug.Log($"Swipe vertical: {(swipeDelta.y > 0 ? "Up" : "Down")}");
            ProcessAction(swipeDelta.y > 0 ? PlayerAction.Jump : PlayerAction.Slide);
        }
    }

    // Map actions to movement logic
    private void ProcessAction(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.MoveLeft:
                MoveToLane(PlayerPoint.Left);
                break;
            case PlayerAction.MoveRight:
                MoveToLane(PlayerPoint.Right);
                break;
            case PlayerAction.Jump:
                PerformJump();
                break;
            case PlayerAction.Slide:
                PerformSlide();
                break;
        }
    }

    // Handle jump logic
    private void PerformJump()
    {
        if (isVerticalInputLocked)
        {
            Debug.Log("Jump blocked: Vertical input locked");
            return;
        }

        if (currentState == PlayerState.Jumping)
        {
            Debug.Log("Jump blocked: Already jumping");
            return;
        }

        bool isJumpFromSlide = currentState == PlayerState.Sliding;
        currentState = PlayerState.Jumping;
        KillVerticalTween();

        float targetY = initialPosition.y + jumpHeight;
        isVerticalInputLocked = isJumpFromSlide; // Lock only for jump-from-slide
        verticalTween = transform.DOMoveY(targetY, jumpDuration / 2)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                verticalTween = transform.DOMoveY(initialPosition.y, jumpDuration / 2)
                    .SetEase(Ease.InQuad)
                    .OnComplete(ResetToIdle);
            });

        // Optional: Trigger jump animation
        // animator.SetTrigger("Jump");
    }

    // Handle slide logic
    private void PerformSlide()
    {
        if (isVerticalInputLocked)
        {
            Debug.Log("Slide blocked: Vertical input locked");
            return;
        }

        if (currentState == PlayerState.Sliding)
        {
            Debug.Log("Slide blocked: Already sliding");
            return;
        }

        bool isSlideFromJump = currentState == PlayerState.Jumping;
        currentState = PlayerState.Sliding;
        KillVerticalTween();

        float targetY = initialPosition.y + slideHeight;
        float slideTime = isSlideFromJump ? slideFromJumpDuration : slideDuration / 2;
        isVerticalInputLocked = isSlideFromJump; // Lock only for slide-from-jump
        verticalTween = transform.DOMoveY(targetY, slideTime)
            .SetEase(isSlideFromJump ? Ease.InQuad : Ease.OutQuad)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(slideDuration / 2, ReturnToIdle);
            });

        // Optional: Trigger slide animation
        // animator.SetTrigger("Slide");
    }

    // Return to idle state
    private void ReturnToIdle()
    {
        verticalTween = transform.DOMoveY(initialPosition.y, slideDuration / 4)
            .SetEase(Ease.InQuad)
            .OnComplete(ResetToIdle);
    }

    // Reset state to idle
    private void ResetToIdle()
    {
        verticalTween = null;
        currentState = PlayerState.Idle;
        isVerticalInputLocked = false;
        ResetLaneAnimationIfCentered();
    }

    // Handle lane movement
    private void MoveToLane(PlayerPoint point)
    {
        PlayerPoint targetLane = validPlayerPoint(point == PlayerPoint.Left);
        Debug.Log($"MoveToLane: currentLane={currentLane}, targetLane={targetLane}");

        if (currentLane == targetLane)
        {
            Debug.Log("MoveToLane: Already at target lane");
            return;
        }

        KillLateralTween();
        SetLaneAnimation(targetLane);

        // Determine next lane
        PlayerPoint nextLane = targetLane;
        if (currentLane == PlayerPoint.Left && targetLane == PlayerPoint.Right ||
            currentLane == PlayerPoint.Right && targetLane == PlayerPoint.Left)
        {
            nextLane = PlayerPoint.Center; // Go through Center
        }

        SwitchToLane(nextLane);
    }

    // Determine valid lane point
    private PlayerPoint validPlayerPoint(bool isLeft)
    {
        return isLeft ? PlayerPoint.Left : PlayerPoint.Right;
    }

    // Set lane animation blend
    private void SetLaneAnimation(PlayerPoint point)
    {
        float targetValue = point switch
        {
            PlayerPoint.Left => -1f,
            PlayerPoint.Right => 1f,
            _ => 0f
        };

        AnimateLaneBlend(targetValue);
        DOVirtual.DelayedCall(laneSwitchTime * 2, () => AnimateLaneBlend(0f), true);
    }

    // Animate lane blend parameter
    private void AnimateLaneBlend(float value)
    {
        DOTween.To(() => animator.GetFloat(laneBlendParameter),
            x => animator.SetFloat(laneBlendParameter, x), value, laneSwitchTime)
            .SetEase(Ease.OutFlash);
    }

    // Switch to target lane
    private void SwitchToLane(PlayerPoint targetLane)
    {
        foreach (var standPoint in lanePoints)
        {
            if (standPoint.playerPoint == targetLane)
            {
                Vector3 targetPos = standPoint.GetPositionPoint();
                targetPos.y = transform.position.y;
                Debug.Log($"SwitchToLane: Moving to {targetLane} at {targetPos}");

                KillLateralTween();
                lateralTween = DOTween.Sequence()
                    .Append(transform.DOMoveX(targetPos.x, laneSwitchTime).SetEase(Ease.OutFlash))
                    .Join(transform.DOMoveZ(targetPos.z, laneSwitchTime).SetEase(Ease.OutFlash))
                    .OnComplete(() =>
                    {
                        lateralTween = null;
                        currentLane = targetLane;
                        Debug.Log($"SwitchToLane: Arrived at {currentLane}");
                    });

                break;
            }
        }
    }

    // Reset lane animation if in center
    private void ResetLaneAnimationIfCentered()
    {
        if (currentLane == PlayerPoint.Center)
        {
            AnimateLaneBlend(0f);
        }
    }

    // Kill active vertical tween
    private void KillVerticalTween()
    {
        if (verticalTween != null)
        {
            verticalTween.Kill();
            verticalTween = null;
            isVerticalInputLocked = false;
        }
    }

    // Kill active lateral tween
    private void KillLateralTween()
    {
        if (lateralTween != null)
        {
            lateralTween.Kill();
            lateralTween = null;
        }
    }

    [Button("Set Stand Points")]
    private void InitStandPoints()
    {
        foreach (var standPoint in lanePoints)
        {
            standPoint.Init();
        }
    }

    // Enum for player actions
    private enum PlayerAction
    {
        MoveLeft,
        MoveRight,
        Jump,
        Slide
    }
}