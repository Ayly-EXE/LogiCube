using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float flySpeed = 7f;
    public float lookSpeed = 2f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("Double Jump Fly Toggle")]
    public float doubleTapDelay = 0.3f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;

    private float pitch = 0f;
    private float verticalVelocity = 0f;

    private bool isFlying = false;
    private float lastSpacePressTime = -10f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Look();
        HandleJumpAndFlyToggle();
        Move();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        transform.Rotate(0f, mouseX, 0f, Space.Self);

        Vector3 bodyEuler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, bodyEuler.y, 0f);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleJumpAndFlyToggle()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        float now = Time.time;
        bool isDoubleTap = now - lastSpacePressTime <= doubleTapDelay;
        lastSpacePressTime = now;

        // Double tap in air => toggle fly
        if (!controller.isGrounded && isDoubleTap)
        {
            isFlying = !isFlying;

            if (isFlying)
            {
                verticalVelocity = 0f;
            }

            return;
        }

        // Normal jump only if grounded and not flying
        if (controller.isGrounded && !isFlying)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 horizontalMove = (transform.right * x + transform.forward * z).normalized;

        if (isFlying)
        {
            float verticalFly = 0f;

            if (Input.GetKey(KeyCode.Space))
                verticalFly += 1f;

            if (Input.GetKey(KeyCode.LeftShift))
                verticalFly -= 1f;

            Vector3 move = horizontalMove * flySpeed + Vector3.up * verticalFly * flySpeed;
            controller.Move(move * Time.deltaTime);
            return;
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalMove * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }
}