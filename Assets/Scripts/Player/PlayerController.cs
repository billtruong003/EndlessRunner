using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator anim;
    [SerializeField] private CapsuleCollider playerCollider;
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Settings")]
    [SerializeField] private float baseForwardSpeed = 10f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float laneDistance = 3f;
    [SerializeField] private float laneSwitchSpeed = 10f;

    [Header("Actions")]
    [SerializeField] private float slideDuration = 1f;
    [SerializeField] private float slideSpeedBoost = 2f;

    // Private variables
    private bool isGrounded;
    private bool isJumping;
    private bool isSliding;
    private float verticalVelocity;
    private int currentLane = 1; // 0 = left, 1 = middle, 2 = right
    private float targetXPosition;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    // Speed management
    private float currentForwardSpeed;
    private float speedMultiplier = 1f;

    private void Start()
    {
        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
        }

        currentLane = 1;
        targetXPosition = 0;
        currentForwardSpeed = baseForwardSpeed;
    }

    private void Update()
    {
        // Get speed from PlayerStat if available
        if (playerStat != null)
        {
            currentForwardSpeed = baseForwardSpeed + playerStat.GetSpeed();
        }

        CheckGround();
        HandleInput();
        HandleGravity();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        // Lane switching
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveLane(false);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveLane(true);
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isSliding)
        {
            Jump();
        }

        // Slide
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && isGrounded && !isSliding)
        {
            StartCoroutine(Slide());
        }

        // Fast fall when sliding in air
        if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && !isGrounded)
        {
            verticalVelocity = -20f;
        }
    }

    private void HandleMovement()
    {
        Vector3 moveVector = -transform.forward * currentForwardSpeed * speedMultiplier;

        // Smooth lane switching
        float newX = Mathf.Lerp(transform.position.x, targetXPosition, Time.fixedDeltaTime * laneSwitchSpeed);

        // Apply vertical velocity only if not grounded or sliding to prevent interference
        if (isGrounded && !isJumping || isSliding)
        {
            moveVector.y = -2f; // Small downward force to stay grounded
        }
        else
        {
            moveVector.y = verticalVelocity;
        }

        // Move the player using velocity to ensure smooth movement
        rb.linearVelocity = moveVector;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    private void HandleGravity()
    {
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
        }
        else if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime * 0.8f; // Slightly reduce gravity effect for better jump feel
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void MoveLane(bool goingRight)
    {
        if (!goingRight)
        {
            currentLane--;
            if (currentLane < 0) currentLane = 0;
        }
        else
        {
            currentLane++;
            if (currentLane > 2) currentLane = 2;
        }

        targetXPosition = (currentLane - 1) * laneDistance;

        // Animation
        anim.SetFloat("Centroid", goingRight ? 1 : -1);
        StartCoroutine(ResetCentroid());
    }

    private IEnumerator ResetCentroid()
    {
        yield return new WaitForSeconds(0.3f);
        anim.SetFloat("Centroid", 0);
    }

    private void Jump()
    {
        verticalVelocity = jumpForce;
        isJumping = true;
        anim.SetTrigger("Jump");
        StartCoroutine(JumpCooldown());
    }

    private IEnumerator JumpCooldown()
    {
        yield return new WaitForSeconds(0.1f);
        isJumping = false;
    }

    private IEnumerator Slide()
    {
        isSliding = true;
        anim.SetTrigger("Slide");

        // Reduce collider size
        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight * 0.5f;
            playerCollider.center = originalColliderCenter - new Vector3(0, originalColliderHeight * 0.25f, 0);
        }

        // Speed boost while sliding
        speedMultiplier = slideSpeedBoost;

        yield return new WaitForSeconds(slideDuration);

        // Reset collider
        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }

        speedMultiplier = 1f;
        isSliding = false;
    }

    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.25f, groundLayer); // Slightly increase check radius

        // Landing
        if (!wasGrounded && isGrounded && !isJumping)
        {
            anim.SetTrigger("Land");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Handle collectables through their own scripts
        if (other.CompareTag("Obstacle"))
        {
            if (playerStat != null && !playerStat.IsShieldActive())
            {
                playerStat.TakeDamage(10);
                // Add hit effect, sound, etc.
            }
        }
    }

    public void SetSpeedMultiplier(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        if (duration > 0)
        {
            StartCoroutine(ResetSpeedMultiplier(duration));
        }
    }

    private IEnumerator ResetSpeedMultiplier(float duration)
    {
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
    }

    // Public methods for external use
    public float GetCurrentSpeed() => currentForwardSpeed * speedMultiplier;
    public int GetCurrentLane() => currentLane;

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}