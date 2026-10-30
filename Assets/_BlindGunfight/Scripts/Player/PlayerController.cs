using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// WASD 이동(화면 기준), 마우스 방향으로 회전. 달리면 발소리(소음)를 낸다.
    /// 캐릭터 모델은 +Z(앞)를 보고 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("속도 (m/s)")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [Tooltip("조준 중 이동 속도 배율")]
        [SerializeField, Range(0.1f, 1f)] private float aimSpeedMultiplier = 0.6f;

        [Header("발소리 반경 (m)")]
        [SerializeField] private float walkNoiseRadius = 1.5f;
        [SerializeField] private float runNoiseRadius = 4f;
        [SerializeField] private float footstepInterval = 0.4f;

        [Header("참조")]
        [SerializeField] private FieldOfView fieldOfView;

        private Rigidbody rb;
        private Stance stance;
        private Vector3 moveInput;
        private float nextFootstepTime;

        /// <summary>요원별 이동 속도 배율</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public bool IsAiming { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            if (fieldOfView == null) fieldOfView = GetComponent<FieldOfView>();
            stance = GetComponent<Stance>();
        }

        private void Update()
        {
            var rig = CameraRig.Main;
            Vector2 input = GameInput.Move;
            // 카메라가 돌아가도 W 는 항상 화면 위쪽이다.
            Quaternion cameraYaw = Quaternion.Euler(0f, rig != null ? rig.Yaw : 0f, 0f);
            moveInput = cameraYaw * new Vector3(input.x, 0f, input.y);

            IsAiming = GameInput.Aim;
            IsCrouching = GameInput.Crouch && !IsAiming;
            IsRunning = GameInput.Run && !IsAiming && !IsCrouching && moveInput.sqrMagnitude > 0.01f;

            if (fieldOfView != null) fieldOfView.Aiming = IsAiming;
            if (stance != null) stance.SetCrouching(IsCrouching);

            if (rig != null)
            {
                Vector3 toMouse = (rig.AimPoint - rb.position).XZ();
                if (toMouse.sqrMagnitude > 0.04f) AimDirection = toMouse.normalized;
            }

            EmitFootsteps();
        }

        private void FixedUpdate()
        {
            float speed = IsRunning ? runSpeed : IsCrouching ? crouchSpeed : walkSpeed;
            if (IsAiming) speed *= aimSpeedMultiplier;
            speed *= SpeedMultiplier;

            rb.linearVelocity = moveInput * speed;
            rb.MoveRotation(Quaternion.Euler(0f, Flat.Yaw(AimDirection), 0f));
        }

        private void EmitFootsteps()
        {
            if (moveInput.sqrMagnitude < 0.01f || IsCrouching || Time.time < nextFootstepTime) return;

            nextFootstepTime = Time.time + footstepInterval;
            NoiseSystem.Emit(rb.position, IsRunning ? runNoiseRadius : walkNoiseRadius, gameObject);
        }
    }
}
