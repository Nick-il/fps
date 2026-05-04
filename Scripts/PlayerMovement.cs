using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    enum State
    {
        WALKING,
        SPRINTING,
        CROUCHING,
        SLIDING,
        ON_AIR,
        IDLE
    }



    [Header("Key Bindings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("References")]
    public Transform groundCheck;
    public Transform orientation;

    [Header("Movement Settings")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;
    public float crouchYScale = 0.5f;
    public float maxSlopeAngle = 45f;
    float startYScale;
    public float airMultiplier = 0.5f;
    bool sprintInput, crouchInput;
    float speed;

    [Header("Ground Check Settings")]
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;
    public float coyoteTimer = 0.3f;
    bool isGrounded;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpCooldown = 0.25f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    bool canJump, jumpInput, jumpHeld;

    // === Private Variables
    State state;
    Rigidbody rb;
    Vector2 inputDir;
    Vector3 moveDir;
    bool onSlope, exitingSlope;
    RaycastHit slopeHit;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        canJump = true;
        startYScale = transform.localScale.y;
        exitingSlope = false;
    }

    void Update()
    {
        // update onSlope since GroundCheck depends on onSlope
        onSlope = OnSlope();
        isGrounded = GroundCheck();

        HandleInput();
        HandleState();
        Crouching();
    }

    void FixedUpdate()
    {
        // ===---=== MOVEMENT ===---===
        float control = isGrounded ? 1f : airMultiplier; // Degree of control while in ground vs in air

        // Get movement direction in 3D from input
        moveDir = orientation.forward * inputDir.y + orientation.right * inputDir.x;
        moveDir.Normalize();

        // Handle slope movement only if we are on slope and not about to jump
        if (onSlope && !exitingSlope)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
            rb.linearVelocity = moveDir * speed * control;
        }
        // Normal Movement without slope handling
        else
        {
            rb.linearVelocity = new Vector3(moveDir.x * speed * control, rb.linearVelocity.y, moveDir.z * speed * control);
        }

        // ===---=== JUMPING ===---===
        if (jumpInput && isGrounded && canJump)
        {
            Jump();
        }

        // Better Jump code for t_ascent > t_descent for better snappier less floaty feel.
        if (rb.linearVelocity.y < 0 && !isGrounded)
        {
            rb.linearVelocity += Vector3.up * (Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(jumpKey))
        {
            rb.linearVelocity += Vector3.up * (Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime);
        }

        rb.useGravity = !onSlope;

        // Debug.Log($"vel={rb.linearVelocity} | mag={rb.linearVelocity.magnitude} | onSlope={onSlope} | slopeNormal={slopeHit.normal} | moveDir={moveDir}");
    }

    void HandleInput()
    {
        inputDir.x = Input.GetAxisRaw("Horizontal");
        inputDir.y = Input.GetAxisRaw("Vertical");
        inputDir.Normalize();

        crouchInput = Input.GetKey(crouchKey);
        sprintInput = Input.GetKey(sprintKey);

        jumpHeld = Input.GetKey(jumpKey);
        if (Input.GetKeyDown(jumpKey))
        {
            jumpInput = true;
        }
    }

    void HandleState()
    {
        if (!isGrounded)
        {
            state = State.ON_AIR;
        }
        else if (crouchInput && isGrounded)
        {
            state = State.CROUCHING;
            speed = crouchSpeed;
        }
        else if (sprintInput && inputDir.y > 0 && isGrounded)
        {
            state = State.SPRINTING;
            speed = sprintSpeed;
        }
        else if (inputDir != Vector2.zero && isGrounded)
        {
            state = State.WALKING;
            speed = walkSpeed;
        }
        else if (isGrounded)
        {
            state = State.IDLE;
            speed = 0f;
        }
    }

    void Jump()
    {
        jumpInput = canJump = false;
        exitingSlope = true;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(orientation.up * jumpForce, ForceMode.Impulse);
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    void ResetJump()
    {
        canJump = true;
        exitingSlope = false;
    }

    bool GroundCheck()
    {
        if (onSlope) return true;
        return Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask
        );
    }

    void Crouching()
    {
        Vector3 newScale = transform.localScale;

        if (Input.GetKeyDown(crouchKey))
        {
            newScale.y = crouchYScale;
            transform.localScale = newScale;
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(crouchKey))
        {
            newScale.y = startYScale;
            transform.localScale = newScale;
        }
    }

    bool OnSlope()
    {
        if (Physics.Raycast(groundCheck.position, Vector3.down, out slopeHit, groundCheckRadius, groundMask))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            onSlope = angle <= maxSlopeAngle && angle != 0;
            return onSlope;
        }
        onSlope = false;
        return onSlope;
    }

    void OnDrawGizmos()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            groundCheck.position,
            groundCheck.position + Vector3.down * groundCheckRadius
        );

        Gizmos.DrawRay(groundCheck.position, moveDir * 5f);
    }
}
