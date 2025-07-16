// FileName: PlayerController.cs

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private Transform groundCheck;

    [Header("Movement Settings")]
    [SerializeField] private float baseForwardSpeed = 10f;
    [SerializeField] private float laneDistance = 3f;
    [SerializeField] private float laneSwitchSpeed = 15f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravityScale = 2.5f;
    [SerializeField] private float fastFallGravityScale = 4f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Sliding")]
    [SerializeField] private float slideDuration = 1f;

    [Header("Flying")]
    [SerializeField] private float flyAltitude = 5f;
    [SerializeField] private float flyTransitionSpeed = 4f;

    // State Machine
    private bool isGrounded;
    private bool isJumping;
    private bool isSliding;
    private bool isFlying;

    // Movement Internals
    private int currentLane = 1; // 0=Left, 1=Middle, 2=Right
    private float targetXPosition;
    private float currentForwardSpeed;
    private Coroutine activeSlideCoroutine;
    private Coroutine activeFlyCoroutine;

    // Collider Management
    private CapsuleCollider playerCollider;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    public float CurrentForwardSpeed => currentForwardSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;

        rb.useGravity = false; // We will handle gravity manually for better control.
    }

    private void Start()
    {
        currentLane = 1;
        targetXPosition = 0;
    }

    private void Update()
    {
        if (playerStat != null)
        {
            currentForwardSpeed = baseForwardSpeed + playerStat.GetSpeed();
        }

        isGrounded = CheckIfGrounded();
        anim.SetBool("IsGrounded", isGrounded);

        HandleInput();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleGravity();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeLane(-1);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeLane(1);
        }

        if (isFlying) return; // Disable jump/slide while flying

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isSliding)
        {
            Jump();
        }

        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && isGrounded && !isSliding)
        {
            StartSlide();
        }
    }

    private void HandleMovement()
    {
        Vector3 targetVelocity = rb.linearVelocity;
        targetVelocity.z = currentForwardSpeed;

        float smoothXPosition = Mathf.Lerp(rb.position.x, targetXPosition, Time.fixedDeltaTime * laneSwitchSpeed);
        rb.MovePosition(new Vector3(smoothXPosition, rb.position.y, rb.position.z));

        if (isFlying)
        {
            float targetY = Mathf.Lerp(rb.position.y, flyAltitude, Time.fixedDeltaTime * flyTransitionSpeed);
            rb.MovePosition(new Vector3(rb.position.x, targetY, rb.position.z));
            targetVelocity.y = 0;
        }

        rb.linearVelocity = targetVelocity;
    }

    private void HandleGravity()
    {
        if (isFlying || isGrounded)
        {
            return;
        }

        float currentGravityScale = (rb.linearVelocity.y < 0 && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)))
            ? fastFallGravityScale
            : gravityScale;

        rb.AddForce(Physics.gravity * currentGravityScale, ForceMode.Acceleration);
    }

    private void ChangeLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
        targetXPosition = (currentLane - 1) * laneDistance;
    }

    private void Jump()
    {
        isJumping = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Reset vertical velocity before jump
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        anim.SetTrigger("Jump");
        StartCoroutine(JumpCooldownRoutine());
    }

    private IEnumerator JumpCooldownRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        isJumping = false;
    }

    private void StartSlide()
    {
        if (activeSlideCoroutine != null)
        {
            StopCoroutine(activeSlideCoroutine);
        }
        activeSlideCoroutine = StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        anim.SetTrigger("Slide");

        playerCollider.height = originalColliderHeight * 0.5f;
        playerCollider.center = new Vector3(0, originalColliderHeight * 0.25f, 0);

        yield return new WaitForSeconds(slideDuration);

        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;

        isSliding = false;
        activeSlideCoroutine = null;
    }

    public void ActivateFly(float duration)
    {
        if (activeFlyCoroutine != null)
        {
            StopCoroutine(activeFlyCoroutine);
        }
        activeFlyCoroutine = StartCoroutine(FlyRoutine(duration));
    }

    private IEnumerator FlyRoutine(float duration)
    {
        isFlying = true;
        isJumping = false;
        if (isSliding) // Cancel slide if active
        {
            StopCoroutine(activeSlideCoroutine);
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
            isSliding = false;
        }

        anim.SetBool("IsFlying", true);

        yield return new WaitForSeconds(duration);

        anim.SetBool("IsFlying", false);
        isFlying = false;
        activeFlyCoroutine = null;
    }

    private bool CheckIfGrounded()
    {
        // Prevent ground check from detecting the player itself or triggers
        return Physics.CheckSphere(groundCheck.position, 0.2f, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (playerStat != null && !playerStat.IsShieldActive())
            {
                playerStat.TakeDamage(10);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}