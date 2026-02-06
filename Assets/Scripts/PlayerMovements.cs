using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float moveSpeed = 10f;   // Movement speed
    public float lookSpeed = 2f;    // Mouse look speed

    void Start()
    {
        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        transform.Rotate(-mouseY, mouseX, 0);

        // Prevent roll
        Vector3 angles = transform.eulerAngles;
        angles.z = 0;
        transform.eulerAngles = angles;

        // Horizontal movement (relative to world y-axis)
        Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.A)) move -= right;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.Space)) move += Vector3.up;       // Move up
        if (Input.GetKey(KeyCode.LeftShift)) move -= Vector3.up;   // Move down

        transform.position += move * moveSpeed * Time.deltaTime;

        // Optional: Press Escape to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
