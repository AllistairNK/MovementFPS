using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump = true;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope = false;

    [Header("Slope Hysteresis")]
    [Tooltip("Number of consecutive FixedUpdate frames the raw slope reading must disagree with the current state before it is allowed to flip. Prevents the movement force from jittering between slope-force and flat-ground force at ramp edges.")]
    public int slopeHysteresisFrames = 2;
    private bool cachedOnSlope;
    private int slopeDisagreeFrames;

    [Header("Debug")]
    public bool debugSlopeLogging = false;

    public Transform orientation;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody playerRb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    public RaycastHit floor;
    public LineRenderer line;
    public Transform pos1;
    public Transform pos2;

    private void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerRb.freezeRotation = true;
        playerRb.interpolation = RigidbodyInterpolation.Interpolate;
        playerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        startYScale = transform.localScale.y;
        Physics.Raycast(transform.position, Vector3.down,out floor, playerHeight * 0.5f + 0.2f, whatIsGround);
    }
    private void FixedUpdate()
    {
        // ground check (moved here so it stays in sync with the physics step that consumes it)
        // SphereCast instead of a single-point Raycast: a point ray can miss right at the
        // seam between two separate ground meshes (e.g. Floor -> Slope), flickering
        // grounded state for a frame with no hysteresis to catch it.
        grounded = Physics.SphereCast(transform.position, 0.2f, Vector3.down, out _, playerHeight * 0.5f + 0.2f, whatIsGround);
        OnSlope(); // refreshes cachedOnSlope for this physics step
        SpeedControl();
        StateHandler();
        //handle drag
        if (grounded)
        {
            playerRb.linearDamping = groundDrag;
        }else
        {
            playerRb.linearDamping = 0;
        }
        MovePlayer();
    }
    private void Update()
    {
        MyInput();

        line.SetPosition(0, pos1.position);
        line.SetPosition(1, GetSlopeMoveDirection());
    }



    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        //Check jump key down
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        //Check crouch key down
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            playerRb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        //Check crouch key up
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    public void StateHandler()
    {
        //Mode - Crouching
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        //Mode - Sprinting
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        //Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        //Mode = Air
        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        //calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        //on slope
        if (cachedOnSlope && !exitingSlope)
        {

            playerRb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f , ForceMode.Force);
            //if(playerRb.velocity.y > 0)
            //{
                playerRb.AddForce(Vector3.down * 80f, ForceMode.Force);
                //transform.TransformDirection(transform.position.x,-0.2f,transform.position.z);
            //}
        }
        //on ground
        if (grounded)
        {
            playerRb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        //in air
        else if(!grounded)
        {
            playerRb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
        //turn off gravity while on slope
        playerRb.useGravity = !cachedOnSlope;
    }

    private void SpeedControl()
    {
        //limit velocity speed
        if (cachedOnSlope && !exitingSlope)
        {
            if(playerRb.linearVelocity.magnitude > moveSpeed)
            {
                playerRb.linearVelocity = playerRb.linearVelocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
            //limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                playerRb.linearVelocity = new Vector3(limitedVel.x, playerRb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        //reset y velocity
        playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        playerRb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        bool rawOnSlope;
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            rawOnSlope = angle < maxSlopeAngle && angle != 0;
        }
        else
        {
            rawOnSlope = false;
        }

        // Hysteresis: only flip the cached state once the raw reading has disagreed
        // for slopeHysteresisFrames in a row. Without this, a single-frame raycast
        // flicker at a ramp edge or mesh seam swaps the movement force (slope-parallel
        // vs. flat-ground) and gravity on/off abruptly, which is what produces the
        // sudden forward "jump" during testing.
        if (rawOnSlope == cachedOnSlope)
        {
            slopeDisagreeFrames = 0;
        }
        else
        {
            slopeDisagreeFrames++;
            if (slopeDisagreeFrames >= slopeHysteresisFrames)
            {
                cachedOnSlope = rawOnSlope;
                slopeDisagreeFrames = 0;
            }
        }

        if (debugSlopeLogging)
        {
            Debug.Log($"[Slope] grounded={grounded} raw={rawOnSlope} cached={cachedOnSlope} angle={Vector3.Angle(Vector3.up, slopeHit.normal):F1}");
        }

        return cachedOnSlope;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        //Debug.DrawLine(transform.position, Vector3.ProjectOnPlane(moveDirection, floor.normal).normalized);
        //Debug.DrawLine(transform.position, Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized);
        //Debug.DrawRay(transform.position, Vector3.ProjectOnPlane(moveDirection, floor.normal).normalized);
        //Debug.Log(Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized);
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
