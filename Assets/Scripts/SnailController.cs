using System.Collections;
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

    [Header("Skateboard")]
    public GameObject skateboardObject;
    public float skateSpeed = 9f;
    public float skateRotationSpeed = 4f;
    public float skateDownhillBonus = 6f;
    public float skateAcceleration = 5f;
    public float skateBraking = 3f;
    public Vector3 skateboardHiddenScale = Vector3.zero;
    public Vector3 skateboardVisibleScale = Vector3.one;
    public float skateTransitionDuration = 0.2f;
    public Vector3 skateHeightOffset = new Vector3(0f, 0.15f, 0f);
    public float skateJumpForce = 8f;
    public float skateJumpCooldown = 0.5f;
    public float defaultSlopeLimit = 70f;
    public float skateSlopeLimit = 85f;

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

    private bool isSkateboarding = false;
    private float currentSkateSpeed = 0f;
    private bool isSkateJumping = false;
    private float skateJumpCooldownTimer = 0f;
    private Vector3 defaultControllerCenter;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        defaultControllerCenter = controller.center;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleSkateToggle();
        HandleMovement();
        HandleLunge();
        HandleSkateJump();
        AlignToSlope();
    }

    void HandleSkateToggle()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            isSkateboarding = !isSkateboarding;
            currentSkateSpeed = isSkateboarding ? moveSpeed : 0f;
            StopCoroutine("SkateTransition");
            StartCoroutine(SkateTransition(isSkateboarding));
        }
    }

    IEnumerator SkateTransition(bool entering)
    {
        skateboardObject.SetActive(true);

        Vector3 startScale = skateboardObject.transform.localScale;
        Vector3 targetScale = entering ? skateboardVisibleScale : skateboardHiddenScale;

        Vector3 startOffset = controller.center;
        Vector3 targetOffset = entering
        ? defaultControllerCenter + skateHeightOffset
        : defaultControllerCenter;

        float elapsed = 0f;
        while (elapsed < skateTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / skateTransitionDuration);
            skateboardObject.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            controller.center = Vector3.Lerp(startOffset, targetOffset, t);
            yield return null;
        }

        skateboardObject.transform.localScale = targetScale;
        controller.center = targetOffset;

        if (!entering)
            skateboardObject.SetActive(false);
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

        // Get slope info for both speed penalty and skate downhill boost
        float slopeSpeedMultiplier = 1f;
        float slopeAngle = 0f;
        bool goingDownhill = false;
        RaycastHit slopeHit;
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 2f))
        {
            slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            slopeSpeedMultiplier = Mathf.Lerp(1f, 0.5f, slopeAngle / 70f);

            // Downhill = moving in the direction the slope descends
            Vector3 slopeDown = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
            goingDownhill = Vector3.Dot(move.normalized, slopeDown) > 0.3f;

            //Raise slope limit when in skatepark tagged surfaces
            bool onSkatepark = slopeHit.collider.gameObject.CompareTag("Skatepark");
            controller.slopeLimit = (isSkateboarding && onSkatepark) ? skateSlopeLimit : defaultSlopeLimit;
        }

        float currentSpeed;

        if (isSkateboarding)
        {
            float targetSpeed = skateSpeed;

            // Downhill bonus
            if (goingDownhill)
                targetSpeed += Mathf.Lerp(0f, skateDownhillBonus, slopeAngle / 45f);

            // Accelerate toward target, or brake if no input
            if (move.magnitude > 0.1f)
                currentSkateSpeed = Mathf.MoveTowards(currentSkateSpeed, targetSpeed, skateAcceleration * Time.deltaTime);
            else
                currentSkateSpeed = Mathf.MoveTowards(currentSkateSpeed, 0f, skateBraking * Time.deltaTime);

            currentSpeed = currentSkateSpeed;

            // Sluggish turning — skate commits to direction
            if (move.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    targetRotation, Time.deltaTime * skateRotationSpeed);
            }
        }
        else
        {
            // Normal movement
            currentSpeed = isLunging ? lungeSpeed : moveSpeed;
            currentSpeed *= slopeSpeedMultiplier;

            if (move.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        if (controller.isGrounded)
        {
            if (velocity.y < 0)
            {
                isSkateJumping = false;
                velocity.y = -groundedGravity;
            }
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }

        controller.Move((move.normalized * currentSpeed + velocity) * Time.deltaTime);
        animator.SetFloat("Speed", move.magnitude);
    }

    void HandleLunge()
    {
        if (lungeCooldownTimer > 0)
        {
            lungeCooldownTimer -= Time.deltaTime;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame &&
            lungeCooldownTimer <= 0 &&
            !isLunging &&
            !isSkateboarding) // disable lunge while skating
        {
            isLunging = true;
            lungeTimer = lungeDuration;
            lungeCooldownTimer = lungeCooldown;
        }

        if (isLunging)
        {
            lungeTimer -= Time.deltaTime;
            if (lungeTimer <= 0)
            {
                isLunging = false;
            }
        }
    }

    void HandleSkateJump()
    {
        if (skateJumpCooldownTimer > 0)
            skateJumpCooldownTimer -= Time.deltaTime;

        if (isSkateboarding &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            controller.isGrounded &&
            skateJumpCooldownTimer <= 0)
        {
            velocity.y = skateJumpForce;
            isSkateJumping = true;
            skateJumpCooldownTimer = skateJumpCooldown;
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