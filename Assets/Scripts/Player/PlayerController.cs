using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This attribute ensures that a Rigidbody component is attached to the GameObject.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // You can set these values in the Unity Inspector.
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float verticalSpeed = 3f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform; // Assign your player's camera here
    [SerializeField] private float mouseSensitivity = 100f;

    [Header("Component References")]
    [SerializeField] private Rigidbody rb;

    private Vector3 moveInput;
    private float xRotation = 0f; // Stores the current up/down rotation of the camera

    // Called when the script is loaded or a value is changed in the Inspector.
    void OnValidate()
    {
        // Automatically find the Rigidbody component if it hasn't been assigned.
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Lock the cursor to the center of the screen and make it invisible.
        // Press 'Esc' during play to show it again.
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // --- MOUSE LOOK ---
        // Get mouse input values.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Calculate the camera's vertical rotation (pitch).
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevents looking behind yourself.

        // Apply rotation to the camera (up and down).
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // Apply rotation to the player body (left and right).
        rb.rotation *= Quaternion.Euler(0f, mouseX, 0f);


        // --- MOVEMENT ---
        // Get horizontal (A/D) and vertical (W/S) input.
        float horizontalInput = Input.GetAxis("Horizontal");
        float forwardInput = Input.GetAxis("Vertical");

        // Create a direction vector based on the player's orientation.
        Vector3 desiredMoveDirection = transform.forward * forwardInput + transform.right * horizontalInput;

        // Get vertical movement input (Space/Shift).
        float verticalMovement = 0f;
        if (Input.GetKey(KeyCode.Space))
        {
            verticalMovement = 1f; // Move Up
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            verticalMovement = -1f; // Move Down
        }

        // Store the final input vector.
        moveInput = new Vector3(desiredMoveDirection.x, verticalMovement, desiredMoveDirection.z);
    }

    void FixedUpdate()
    {
        // --- APPLY MOVEMENT ---
        // We apply movement in FixedUpdate for consistent physics calculations.

        // Calculate target velocity for horizontal/forward movement.
        Vector3 horizontalVelocity = new Vector3(moveInput.x * moveSpeed, 0, moveInput.z * moveSpeed);
        
        // Calculate target velocity for vertical movement.
        float verticalVelocity = moveInput.y * verticalSpeed;

        // Set the Rigidbody's velocity.
        rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
    }
}