using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Serialized fields for Unity Inspector
    [Header("References")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;

    [Header("Animation")]
    [SerializeField] private string laneBlendParameter = "Centroid";

    [Header("Movement Settings")]
    [SerializeField] private float laneSwitchSpeed = 20f;
    [SerializeField] private float laneSwitchSmoothTime = 0.1f;
    [SerializeField] private PlayerPoint currentLane = PlayerPoint.None;
    [SerializeField] private StandPoint[] lanePoints;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpCooldown = 0.5f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float fastFallMultiplier = 4f;
    [SerializeField] private float groundCheckDistance = 0.5f; // Tăng khoảng cách raycast
    [SerializeField] private LayerMask groundLayer; // Layer của mặt đất

    [Header("Slide Settings")]
    [SerializeField] private float slideDuration = 0.5f;

    [Header("Camera Settings")]
    [SerializeField] private float cameraFollowSpeed = 0.3f;

    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float maxSwipeTime = 0.5f;

    // Player state management
    private enum PlayerState { Idle, Jumping, Sliding }
    private PlayerState currentState = PlayerState.Idle;
    private Vector3 initialPosition;
    private bool isVerticalInputLocked;
    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool isTouching;
    private float lastJumpTime;
    private Vector3 targetLanePosition;
    private bool isMovingToLane;
    private Vector3 laneVelocity;
    private bool isFastFalling;
    private bool isGround; // Biến lưu trạng thái chạm đất

    private void Start()
    {
        initialPosition = transform.position;
        if (currentLane == PlayerPoint.None)
            currentLane = PlayerPoint.Center;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        targetLanePosition = transform.position;

        // Khởi tạo isGround
        isGround = false;
    }

    private void Update()
    {
        // Cập nhật trạng thái chạm đất mỗi frame
        isGround = IsGrounded();
        Debug.Log($"IsGrounded: {isGround}");

        ProcessInput();
        cameraController.FollowPosition(transform.position, cameraFollowSpeed);
        // Điều chỉnh trọng lực khi nhảy
        if (rb.linearVelocity.y < 0 && currentState == PlayerState.Jumping)
        {
            float multiplier = isFastFalling ? fastFallMultiplier : fallMultiplier;
            rb.AddForce(Physics.gravity * (multiplier - 1) * rb.mass);
        }

        // Chuyển động làn
        if (isMovingToLane)
        {
            MoveToTargetLane();
        }
    }
    private void ProcessInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            ProcessAction(PlayerAction.MoveLeft);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            ProcessAction(PlayerAction.MoveRight);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            ProcessAction(PlayerAction.Jump);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            ProcessAction(PlayerAction.Slide);

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

    private void ProcessSwipe(Vector2 swipeDelta)
    {
        float absX = Mathf.Abs(swipeDelta.x);
        float absY = Mathf.Abs(swipeDelta.y);

        if (absX > absY)
        {
            Debug.Log($"Swipe horizontal: {(swipeDelta.x > 0 ? "Right" : "Left")}");
            ProcessAction(swipeDelta.x > 0 ? PlayerAction.MoveRight : PlayerAction.MoveLeft);
        }
        else
        {
            Debug.Log($"Swipe vertical: {(swipeDelta.y > 0 ? "Up" : "Down")}");
            ProcessAction(swipeDelta.y > 0 ? PlayerAction.Jump : PlayerAction.Slide);
        }
    }

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

    private void PerformJump()
    {
        if (isVerticalInputLocked || Time.time - lastJumpTime < jumpCooldown || !isGround)
        {
            Debug.Log("Jump blocked: " + (isVerticalInputLocked ? "Input locked" : Time.time - lastJumpTime < jumpCooldown ? "Cooldown" : "Not grounded"));
            return;
        }

        if (currentState == PlayerState.Jumping)
        {
            Debug.Log("Jump blocked: Already jumping");
            return;
        }

        currentState = PlayerState.Jumping;
        isFastFalling = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;

        // Bắt đầu coroutine CheckLanding
        StartCoroutine(CheckLanding());
    }

    private bool IsGrounded()
    {
        Vector3 rayOrigin = transform.position + (Vector3.up * 0.1f); // Bắt đầu từ chân nhân vật
        Ray ray = new Ray(rayOrigin, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, groundCheckDistance, groundLayer);

        if (hits.Length > 0)
        {
            Debug.Log($"RaycastAll found {hits.Length} hits:");
            foreach (var hit in hits)
            {
                Debug.Log($"Hit: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Position: {hit.point})");
            }
            return true;
        }
        else
        {
            Debug.Log($"RaycastAll found no hits. Ray origin: {rayOrigin}, Distance: {groundCheckDistance}");
            return false;
        }
    }

    private IEnumerator CheckLanding()
    {
        Debug.Log("CheckLanding: Waiting for falling...");
        yield return new WaitUntil(() => rb.linearVelocity.y < 0);
        Debug.Log("CheckLanding: Falling detected, waiting for ground...");

        yield return new WaitUntil(() => isGround);
        Debug.Log("CheckLanding: Grounded, resetting to Idle");

        ResetToIdle();
    }

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

        if (currentState == PlayerState.Jumping)
        {
            isFastFalling = true;
            return;
        }

        currentState = PlayerState.Sliding;
        StartCoroutine(SlideCoroutine());
    }

    private IEnumerator SlideCoroutine()
    {
        yield return new WaitForSeconds(slideDuration);
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        Debug.Log("ResetToIdle: Setting state to Idle");
        currentState = PlayerState.Idle;
        isVerticalInputLocked = false;
        isFastFalling = false;
        ResetLaneAnimationIfCentered();
    }

    private void MoveToLane(PlayerPoint point)
    {
        PlayerPoint targetLane = validPlayerPoint(point == PlayerPoint.Left);
        Debug.Log($"MoveToLane: currentLane={currentLane}, targetLane={targetLane}");

        if (currentLane == targetLane)
        {
            Debug.Log("MoveToLane: Already at target lane");
            return;
        }

        SetLaneAnimation(targetLane);

        PlayerPoint nextLane = targetLane;
        if (currentLane == PlayerPoint.Left && targetLane == PlayerPoint.Right ||
            currentLane == PlayerPoint.Right && targetLane == PlayerPoint.Left)
        {
            nextLane = PlayerPoint.Center;
        }

        SwitchToLane(nextLane);
    }

    private PlayerPoint validPlayerPoint(bool isLeft)
    {
        return isLeft ? PlayerPoint.Left : PlayerPoint.Right;
    }

    private void SetLaneAnimation(PlayerPoint point)
    {
        float targetValue = point switch
        {
            PlayerPoint.Left => -1f,
            PlayerPoint.Right => 1f,
            _ => 0f
        };

        StartCoroutine(AnimateLaneBlend(targetValue));
    }

    private IEnumerator AnimateLaneBlend(float value)
    {
        float startValue = animator.GetFloat(laneBlendParameter);
        float elapsed = 0f;

        while (elapsed < laneSwitchSmoothTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / laneSwitchSmoothTime);
            animator.SetFloat(laneBlendParameter, Mathf.Lerp(startValue, value, t));
            yield return null;
        }

        yield return new WaitForSeconds(laneSwitchSmoothTime);
        StartCoroutine(AnimateLaneBlend(0f));
    }

    private void SwitchToLane(PlayerPoint targetLane)
    {
        foreach (var standPoint in lanePoints)
        {
            if (standPoint.playerPoint == targetLane)
            {
                targetLanePosition = standPoint.GetPositionPoint();
                targetLanePosition.y = transform.position.y;
                isMovingToLane = true;
                currentLane = targetLane;
                laneVelocity = Vector3.zero;
                Debug.Log($"SwitchToLane: Moving to {targetLane} at {targetLanePosition}");
                break;
            }
        }
    }

    private void MoveToTargetLane()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(targetLanePosition.x, transform.position.y, targetLanePosition.z);

        Vector3 newPosition = Vector3.SmoothDamp(
            currentPos,
            targetPos,
            ref laneVelocity,
            laneSwitchSmoothTime,
            laneSwitchSpeed,
            Time.fixedDeltaTime
        );

        rb.MovePosition(new Vector3(newPosition.x, transform.position.y, newPosition.z));

        float distance = Vector3.Distance(
            new Vector3(currentPos.x, 0, currentPos.z),
            new Vector3(targetPos.x, 0, currentPos.z)
        );

        if (distance < 0.02f)
        {
            isMovingToLane = false;
            rb.MovePosition(new Vector3(targetPos.x, transform.position.y, targetPos.z));
            laneVelocity = Vector3.zero;
            Debug.Log($"SwitchToLane: Arrived at {currentLane}");
        }
    }

    private void ResetLaneAnimationIfCentered()
    {
        if (currentLane == PlayerPoint.Center)
        {
            StartCoroutine(AnimateLaneBlend(0f));
        }
    }

    [ContextMenu("Set Stand Points")]
    private void InitStandPoints()
    {
        foreach (var standPoint in lanePoints)
        {
            standPoint.Init();
        }
    }

    private enum PlayerAction
    {
        MoveLeft,
        MoveRight,
        Jump,
        Slide
    }

    // Hỗ trợ debug raycast trong Editor
    private void OnDrawGizmos()
    {
        Vector3 rayOrigin = transform.position + (Vector3.up * 0.1f);
        Vector3 rayEnd = rayOrigin + Vector3.down * groundCheckDistance;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayEnd);
    }
}