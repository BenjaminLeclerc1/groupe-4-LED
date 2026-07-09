using UnityEngine;
using UnityEngine.InputSystem;

namespace LedShow.Gameplay
{
    // Polls Keyboard/Gamepad directly (same approach as GameManager) rather than
    // through a generated InputActions asset, so Space, Up Arrow and gamepad
    // button A all feel like the same "jump" signal regardless of device.
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerJump : MonoBehaviour
    {
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundMask;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!JumpWasPressedThisFrame() || !IsGrounded())
            {
                return;
            }

            body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        private static bool JumpWasPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }

        private bool IsGrounded()
        {
            return groundCheck == null || Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);
        }
    }
}
