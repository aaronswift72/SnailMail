using UnityEngine;
using UnityEngine.InputSystem;

public class SnailController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotationSpeed = 100f;
    public float slopeAlignSpeed = 10f;
    public float gravity = 20f;
    public float groundedGravity = 2f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.wKey.isPressed) vertical = 1f;
            if (Keyboard.current.sKey.isPressed) vertical = -1f;
            if (Keyboard.current.aKey.isPressed) horizontal = -1f;
            if (Keyboard.current.dKey.isPressed) horizontal = 1f;

            input = new Vector2(horizontal, vertical);
        }

        Vector3 move = transform.forward * input.y;

        // Apply gravity
        if (controller.isGrounded)
        {
            velocity.y = -groundedGravity;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }

        controller.Move((move * moveSpeed + velocity) * Time.deltaTime);
        transform.Rotate(0, input.x * rotationSpeed * Time.deltaTime, 0);

        animator.SetFloat("Speed", Mathf.Abs(input.y));

        AlignToSlope();
    }

    void AlignToSlope()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * slopeAlignSpeed);
        }
    }
}