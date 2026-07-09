using UnityEngine;
using UnityEngine.InputSystem;

namespace LedShow.Gameplay
{
    // No Rigidbody: PhysX gravity/impulses are what usually cause a bounce on
    // landing (velocity overshoots below the ground, then gets corrected next
    // frame). Instead the vertical position is fully driven by a raycast ground
    // sample plus the jump curve, so landing is a direct assignment, not a
    // physics correction, and can never overshoot.
    [RequireComponent(typeof(CapsuleCollider))]
    public class SkierController : MonoBehaviour
    {
        [SerializeField]
        private AnimationCurve jumpCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));

        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float jumpDuration = 0.6f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundCheckDistance = 0.2f;

        private CapsuleCollider capsule;
        private float capsuleRadius;
        private float jumpElapsed;

        public bool IsJumping { get; private set; }

        private void Awake()
        {
            capsule = GetComponent<CapsuleCollider>();
            capsuleRadius = capsule.radius;
        }

        private void Update()
        {
            if (!IsJumping && JumpWasPressedThisFrame() && IsGrounded())
            {
                IsJumping = true;
                jumpElapsed = 0f;
            }

            if (IsJumping)
            {
                UpdateJump();
            }
            else
            {
                SnapToGround();
            }
        }

        private void UpdateJump()
        {
            jumpElapsed += Time.deltaTime;

            if (jumpElapsed >= jumpDuration)
            {
                IsJumping = false;
                SnapToGround();
                return;
            }

            float normalizedTime = jumpElapsed / jumpDuration;
            float verticalOffset = jumpCurve.Evaluate(normalizedTime) * jumpHeight;
            SetHeight(SampleGroundHeight() + verticalOffset);
        }

        private void SnapToGround()
        {
            SetHeight(SampleGroundHeight());
        }

        private void SetHeight(float height)
        {
            Vector3 position = transform.position;
            position.y = height;
            transform.position = position;
        }

        private float SampleGroundHeight()
        {
            Vector3 origin = transform.position + Vector3.up * capsuleRadius;
            float castDistance = capsuleRadius + groundCheckDistance + 10f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castDistance, groundMask))
            {
                return hit.point.y + capsuleRadius;
            }

            return transform.position.y;
        }

        private bool IsGrounded()
        {
            Vector3 origin = transform.position + Vector3.up * capsuleRadius;
            return Physics.Raycast(origin, Vector3.down, capsuleRadius + groundCheckDistance, groundMask);
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
    }
}
