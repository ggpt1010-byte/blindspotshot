using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 블라인드스팟식 쿼터뷰 카메라. 비스듬히 내려다보며 플레이어를 따라가고, 마우스 쪽으로 조금 앞을 보여준다.
    /// 마우스가 가리키는 바닥 위치(AimPoint)도 여기서 계산한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Main { get; private set; }

        [SerializeField] private Transform target;

        [Header("각도")]
        [Tooltip("내려다보는 각도. 90이면 완전 탑다운")]
        [SerializeField, Range(30f, 90f)] private float pitch = 55f;
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float distance = 20f;
        [SerializeField] private float fieldOfView = 35f;

        [Header("따라가기")]
        [SerializeField] private float smoothTime = 0.12f;
        [Tooltip("마우스 방향으로 화면을 얼마나 당길지 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float lookAhead = 0.3f;
        [SerializeField] private float maxLookAhead = 5f;

        [Header("맵 경계")]
        [Tooltip("카메라 중심이 맵 밖(검은 영역)을 비추지 않도록 이 범위 안에 묶는다")]
        [SerializeField] private bool clampToBounds = true;
        [SerializeField] private Vector2 boundsMin = new Vector2(-11f, -8.5f);
        [SerializeField] private Vector2 boundsMax = new Vector2(11f, 6f);

        [Header("조준 평면")]
        [Tooltip("마우스 위치를 이 높이의 수평면에 투영한다 (총구 높이와 맞춘다)")]
        [SerializeField] private float aimPlaneHeight = 1.1f;

        private Camera cam;
        private Vector3 focus;
        private Vector3 velocity;

        public Camera Camera => cam;
        public float Yaw => yaw;
        /// <summary>마우스가 가리키는 조준 평면 위의 점</summary>
        public Vector3 AimPoint { get; private set; }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = fieldOfView;
            if (target != null) focus = Clamp(target.position.XZ());
        }

        private void Start()
        {
            // 첫 프레임부터 플레이어 쪽을 비추도록 바로 자리를 잡는다
            if (target == null) return;
            focus = Clamp(target.position.XZ());
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }

        private void OnEnable() => Main = this;

        private void OnDisable()
        {
            if (Main == this) Main = null;
        }

        private void Update()
        {
            // 조준점은 플레이어 입력보다 먼저 계산되어야 하므로 Update 에서 갱신한다.
            Ray ray = cam.ScreenPointToRay(GameInput.MouseScreenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, aimPlaneHeight, 0f));
            if (plane.Raycast(ray, out float enter)) AimPoint = ray.GetPoint(enter);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPos = target.position.XZ();
            Vector3 offset = Vector3.ClampMagnitude((AimPoint.XZ() - targetPos) * lookAhead, maxLookAhead);
            focus = Vector3.SmoothDamp(focus, Clamp(targetPos + offset), ref velocity, smoothTime);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }

        private Vector3 Clamp(Vector3 p)
        {
            if (!clampToBounds) return p;
            return new Vector3(Mathf.Clamp(p.x, boundsMin.x, boundsMax.x), p.y, Mathf.Clamp(p.z, boundsMin.y, boundsMax.y));
        }
    }
}
