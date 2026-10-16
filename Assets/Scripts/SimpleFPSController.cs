using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    [Header("Raton")]
    [Tooltip("Grados por pixel de raton (delta del nuevo Input System).")]
    public float mouseSensitivity = 0.15f;
    public Camera playerCamera;
    public float eyeHeight = 1.6f;
    public float eyeForwardOffset = 0.2f;

    private CharacterController controller;
    private float pitch = 0f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            var camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null) playerCamera = camObj.GetComponent<Camera>();
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // Bloqueo / desbloqueo del cursor
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Rotacion con raton (nuevo Input System)
        Vector2 lookDelta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
        float mouseX = lookDelta.x * mouseSensitivity;
        float mouseY = lookDelta.y * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Movimiento WASD (nuevo Input System)
        float h = 0f; // A/D
        float v = 0f; // W/S
        if (kb != null)
        {
            if (kb.wKey.isPressed) v += 1f;
            if (kb.sKey.isPressed) v -= 1f;
            if (kb.dKey.isPressed) h += 1f;
            if (kb.aKey.isPressed) h -= 1f;
        }
        Vector3 move = transform.right * h + transform.forward * v;
        if (move.sqrMagnitude > 1f) move.Normalize();
        move *= moveSpeed;

        // Gravedad + salto con espacio
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (kb != null && kb.spaceKey.wasPressedThisFrame && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
}
