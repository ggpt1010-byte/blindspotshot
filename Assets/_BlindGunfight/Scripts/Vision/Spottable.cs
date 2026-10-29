using System.Collections.Generic;
using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 스포팅 대상. 표시되면 머리 위에 마커가 뜨고, 시야 밖으로 나가면
    /// 마지막으로 본 위치에 흐린 마커가 남았다가 사라진다.
    /// 플레이어가 쏴서 맞혀도 자동으로 표시된다. 마커는 항상 카메라를 향한다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Spottable : MonoBehaviour
    {
        public static readonly List<Spottable> All = new List<Spottable>();

        [Header("마커")]
        [SerializeField] private Sprite spottedSprite;
        [SerializeField] private Sprite lastSeenSprite;
        [Tooltip("어둠 속에서도 보이도록 Unlit 재질을 넣는다")]
        [SerializeField] private Material markerMaterial;
        [SerializeField] private float markerHeight = 3.4f; // 이름표 글자 위
        [SerializeField] private float lastSeenHeight = 1f;
        [SerializeField] private float markerScale = 0.6f;
        [SerializeField] private Color markerColor = new Color(1f, 0.27f, 0.22f, 1f);

        [Header("기억")]
        [Tooltip("시야에서 사라진 뒤 마지막 위치 마커가 남는 시간(초)")]
        [SerializeField] private float memoryDuration = 6f;

        private Health health;
        private SpriteRenderer marker;
        private bool spotted;
        private float lastSeenTime;
        private Vector3 lastSeenPosition;

        public bool IsSpotted => spotted;

        private void Awake()
        {
            health = GetComponent<Health>();

            var go = new GameObject(name + " Marker");
            marker = go.AddComponent<SpriteRenderer>();
            marker.sortingOrder = 50;
            if (markerMaterial != null) marker.sharedMaterial = markerMaterial;
            go.transform.localScale = Vector3.one * markerScale;
            marker.enabled = false;
        }

        private void OnEnable()
        {
            All.Add(this);
            health.Damaged += OnDamaged;
            health.Died += Clear;
        }

        private void OnDisable()
        {
            All.Remove(this);
            health.Damaged -= OnDamaged;
            health.Died -= Clear;
        }

        private void OnDestroy()
        {
            if (marker != null) Destroy(marker.gameObject);
        }

        public void Spot()
        {
            if (health.IsDead) return;
            spotted = true;
            lastSeenTime = Time.time;
            lastSeenPosition = transform.position;
        }

        public void Clear()
        {
            spotted = false;
            if (marker != null) marker.enabled = false;
        }

        private void OnDamaged(float amount, GameObject source)
        {
            if (source != null && source.CompareTag("Player")) Spot();
        }

        private void LateUpdate()
        {
            if (!spotted) return;

            var cam = Camera.main;
            Quaternion facing = cam != null ? cam.transform.rotation : Quaternion.identity;

            var view = FieldOfView.Player;
            bool visible = view != null && view.CanSee(transform.position);
            if (visible)
            {
                lastSeenTime = Time.time;
                lastSeenPosition = transform.position;

                float bob = Mathf.Sin(Time.time * 6f) * 0.08f;
                marker.transform.SetPositionAndRotation(lastSeenPosition.XZ() + Vector3.up * (markerHeight + bob), facing);
                marker.sprite = spottedSprite;
                marker.color = markerColor;
                marker.enabled = true;
                return;
            }

            float elapsed = Time.time - lastSeenTime;
            if (elapsed > memoryDuration)
            {
                Clear();
                return;
            }

            // 마지막으로 본 자리에 남는 흐린 마커
            float alpha = Mathf.Lerp(0.7f, 0.15f, elapsed / memoryDuration);
            marker.transform.SetPositionAndRotation(lastSeenPosition.XZ() + Vector3.up * lastSeenHeight, facing);
            marker.sprite = lastSeenSprite != null ? lastSeenSprite : spottedSprite;
            marker.color = new Color(markerColor.r, markerColor.g, markerColor.b, alpha);
            marker.enabled = true;
        }
    }
}
