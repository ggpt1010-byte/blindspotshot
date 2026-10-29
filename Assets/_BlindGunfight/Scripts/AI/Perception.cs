using System;
using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 적 AI의 눈과 귀. 플레이어와 같은 시야 규칙으로 적대 세력을 찾고, 소음과 피격을 듣는다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Perception : MonoBehaviour
    {
        [Header("시야")]
        [SerializeField] private float viewAngle = 100f;
        [SerializeField] private float viewDistance = 10f;
        [SerializeField] private float closeRadius = 1.5f;
        [SerializeField] private LayerMask obstacleMask;
        [Tooltip("시야 검사 주기(초). 적이 많으면 늘린다.")]
        [SerializeField] private float scanInterval = 0.1f;

        [Header("청각")]
        [Tooltip("소음 반경에 곱하는 값. 좀비는 1.5 처럼 크게")]
        [SerializeField] private float hearingMultiplier = 1f;

        private static readonly Collider[] Buffer = new Collider[32];

        private Health self;
        private float nextScanTime;

        public Health Target { get; private set; }
        public Vector3 LastSeenPosition { get; private set; }
        public float LastSeenTime { get; private set; } = float.NegativeInfinity;

        /// <summary>소음이나 피격으로 알게 된 위치</summary>
        public event Action<Vector3> Heard;

        private void Awake()
        {
            self = GetComponent<Health>();
        }

        private void OnEnable()
        {
            NoiseSystem.NoiseMade += OnNoise;
            self.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            NoiseSystem.NoiseMade -= OnNoise;
            self.Damaged -= OnDamaged;
            Target = null;
        }

        private void Update()
        {
            if (Target != null && Target.IsDead) Target = null;
            if (Time.time < nextScanTime) return;
            nextScanTime = Time.time + scanInterval;

            Target = FindVisibleTarget();
            if (Target != null)
            {
                LastSeenPosition = Target.transform.position;
                LastSeenTime = Time.time;
            }
        }

        private Health FindVisibleTarget()
        {
            Vector3 origin = transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin + Vector3.up, Mathf.Max(viewDistance, closeRadius), Buffer,
                                                      Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            Health best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var other = Buffer[i].GetComponentInParent<Health>();
                if (other == null || other == self || other.IsDead) continue;
                if (!Factions.AreHostile(self.Faction, other.Faction)) continue;

                Vector3 pos = other.transform.position;
                if (!VisionMath.CanSee(origin, transform.forward, viewAngle, viewDistance, closeRadius, pos, obstacleMask)) continue;

                float d = Flat.Distance(pos, origin);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = other;
                }
            }
            return best;
        }

        private void OnNoise(Vector3 position, float radius, GameObject source)
        {
            if (source != null && source.transform.IsChildOf(transform)) return;
            if (Flat.Distance(position, transform.position) > radius * hearingMultiplier) return;
            Heard?.Invoke(position);
        }

        private void OnDamaged(float amount, GameObject source)
        {
            // 맞으면 쏜 쪽을 안다.
            Heard?.Invoke(source != null ? source.transform.position : transform.position);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            VisionMath.DrawConeGizmo(transform.position, transform.forward, viewAngle, viewDistance, closeRadius);
        }
#endif
    }
}
