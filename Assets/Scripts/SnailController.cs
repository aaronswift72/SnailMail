using UnityEngine;
using UnityEngine.InputSystem;

public class SnailController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float slopeAlignSpeed = 8f;

    [Header("Lunge")]
    public float lungeSpeed = 8f;
    public float lungeDuration = 0.3f;
    public float lungeCooldown = 2f;

    [Header("Physics")]
    public float gravity = 20f;
    public float groundedGravity = 2f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private Camera mainCamera;

    private bool isLunging = false;
    private float lungeTimer = 0f;
    private float lungeCooldownTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // Lock and hide cursor for camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleLunge();
        AlignToSlope();
    }

    void HandleMovement()
    {
        float vertical = 0f;
        float horizontal = 0f;

        if (Keyboard.current.wKey.isPressed) vertical = 1f;
        if (Keyboard.current.sKey.isPressed) vertical = -1f;
        if (Keyboard.current.aKey.isPressed) horizontal = -1f;
        if (Keyboard.current.dKey.isPressed) horizontal = 1f;

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 move = cameraForward * vertical + cameraRight * horizontal;

        // Get current slope angle and reduce speed accordingly
        float slopeSpeedMultiplier = 1f;
        RaycastHit slopeHit;
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 2f))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            slopeSpeedMultiplier = Mathf.Lerp(1f, 0.5f, slopeAngle / 70f);
        }

        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation,
            targetRotation, Time.deltaTime * rotationSpeed);
        }

        if (controller.isGrounded)
        {
            velocity.y = -groundedGravity;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }

        float currentSpeed = isLunging ? lungeSpeed : moveSpeed;
        // Apply slope multiplier to speed
        currentSpeed *= slopeSpeedMultiplier;

        controller.Move((move * currentSpeed + velocity) * Time.deltaTime);
        animator.SetFloat("Speed", move.magnitude);
    }

    void HandleLunge()
    {
        // Count down cooldown
        if (lungeCooldownTimer > 0)
        {
            lungeCooldownTimer -= Time.deltaTime;
        }

        // Start lunge on spacebar if not on cooldown
        if (Keyboard.current.spaceKey.wasPressedThisFrame &&
            lungeCooldownTimer <= 0 &&
            !isLunging)
        {
            isLunging = true;
            lungeTimer = lungeDuration;
            lungeCooldownTimer = lungeCooldown;
            // Trigger lunge animation here later
            // animator.SetTrigger("Lunge");
        }

        // Count down lunge duration
        if (isLunging)
        {
            lungeTimer -= Time.deltaTime;
            if (lungeTimer <= 0)
            {
                isLunging = false;
            }
        }
    }

    void AlignToSlope()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up,
                hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                targetRotation, Time.deltaTime * slopeAlignSpeed);
        }
    }

    // Optional: unlock cursor when pressing Escape
    // useful for testing in the editor
    void LateUpdate()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}