using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player input via the new Input System and feeds it to PlayerController.
/// Uses direct key reads instead of generated action maps for simplicity.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerController playerController;
    private Camera mainCamera;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Title screen: any key / click starts the game
        if (!GameManager.Instance.GameStarted)
        {
            if (AnyInputThisFrame())
            {
                GameManager.Instance.StartGame();
            }
            playerController?.OnMoveInput(Vector2.zero);
            return;
        }

        if (playerController == null || GameManager.Instance.IsGameOver || Time.timeScale < 0.01f)
        {
            playerController?.OnMoveInput(Vector2.zero);
            return;
        }

        // WASD movement via Input System
        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y = 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y = -1;
            if (Keyboard.current.aKey.isPressed) moveInput.x = -1;
            if (Keyboard.current.dKey.isPressed) moveInput.x = 1;
        }
        // Normalize for diagonal movement
        moveInput = moveInput.normalized;
        playerController.OnMoveInput(moveInput);

        // Dash on Space
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            playerController.Dash();
        }

        // Sprint on Shift (hold)
        if (Keyboard.current != null)
        {
            bool sprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            playerController.SetSprinting(sprintHeld);
        }

        // Mouse aim
        if (mainCamera != null && Mouse.current != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
            playerController.OnAimInput(mouseWorldPos);
        }
    }

    bool AnyInputThisFrame()
    {
        if (Keyboard.current == null) return false;
        // Check any key was pressed this frame, excluding synthetic keys
        foreach (var key in Keyboard.current.allKeys)
        {
            if (key.wasPressedThisFrame) return true;
        }
        // Also check mouse
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame) return true;
        }
        return false;
    }
}
