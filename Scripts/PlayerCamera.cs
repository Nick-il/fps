using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Vector2 sensitivity = new Vector2(100f, 100f);

    public Transform orientation;
    public Transform cameraPosition;

    float xRotation;
    float yRotation;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Update the camera's position to match the cameraPosition transform
        transform.position = cameraPosition.position;

        // Get mouse input and apply sensitivity and deltaTime
        float mouseX = Input.GetAxis("Mouse X") * sensitivity.x * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity.y * Time.deltaTime;

        // Update the rotation angles based on mouse input
        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply the rotations to the camera and orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
