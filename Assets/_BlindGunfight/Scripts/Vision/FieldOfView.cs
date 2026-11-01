using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 플레이어의 시야. 화면에서는 눈높이의 Spot Light 가 부채꼴로 비추고 벽이 그림자를 만든다.
    /// 적이 보이는지는 CanSee 로 판정한다. 앞쪽 = transform.forward (+Z).
    /// </summary>
    public class FieldOfView : MonoBehaviour
    {
        public static FieldOfView Player { get; private set; }

        [SerializeField] private bool isPlayerView = true;

        [Header("기본 시야")]
        [SerializeField] private float viewAngle = 90f;
        [SerializeField] private float viewDistance = 12f; // 2026-10-30 직접 플레이: 7m는 답답해서 다시 넓힘
        [SerializeField] private float closeRadius = 2f;

        [Header("조준 시야 (우클릭)")]
        [SerializeField] private float aimAngle = 50f;
        [SerializeField] private float aimDistance = 16f;
        [SerializeField] private float transitionSpeed = 10f;

        [Header("벽")]
        [SerializeField] private LayerMask obstacleMask;

        [Header("화면 표시 (선택)")]
        [Tooltip("부채꼴 시야를 비추는 Spot Light (눈높이, 앞을 향하게)")]
        [SerializeField] private Light viewLight;

        public bool Aiming { get; set; }
        public float CurrentAngle { get; private set; }
        public float CurrentDistance { get; private set; }
        public LayerMask ObstacleMask => obstacleMask;

        private void Awake()
        {
            CurrentAngle = viewAngle;
            CurrentDistance = viewDistance;
        }

        private void OnEnable()
        {
            if (isPlayerView) Player = this;
        }

        private void OnDisable()
        {
            if (Player == this) Player = null;
        }

        private void Update()
        {
            float targetAngle = Aiming ? aimAngle : viewAngle;
            float targetDistance = Aiming ? aimDistance : viewDistance;
            float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
            CurrentAngle = Mathf.Lerp(CurrentAngle, targetAngle, t);
            CurrentDistance = Mathf.Lerp(CurrentDistance, targetDistance, t);

            if (viewLight != null)
            {
                viewLight.spotAngle = CurrentAngle;
                viewLight.innerSpotAngle = CurrentAngle * 0.8f;
                // URP 조명은 range 끝에서 0으로 떨어지므로, 판정 거리보다 여유를 둬서 끝까지 밝게 보이게 한다
                viewLight.range = CurrentDistance + 4f;
            }
        }

        public bool CanSee(Vector3 point)
        {
            return VisionMath.CanSee(transform.position, transform.forward, CurrentAngle, CurrentDistance,
                                     closeRadius, point, obstacleMask);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            float angle = Application.isPlaying ? CurrentAngle : viewAngle;
            float distance = Application.isPlaying ? CurrentDistance : viewDistance;
            VisionMath.DrawConeGizmo(transform.position, transform.forward, angle, distance, closeRadius);
        }
#endif
    }
}
