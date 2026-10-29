using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 무장 인간: 순찰 → 의심(소리 확인) → 교전 → 수색(마지막 목격 위치) → 순찰.
    /// 교전에 들어가면 외쳐서 근처 동료를 부른다. (외침은 좀비도 듣는다.)
    /// </summary>
    [RequireComponent(typeof(EnemyMotor), typeof(Perception), typeof(Health))]
    public class SoldierAI : MonoBehaviour
    {
        private enum State { Patrol, Suspicious, Combat, Search }

        [SerializeField] private Gun gun;

        [Header("순찰")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float waypointWait = 1.5f;

        [Header("교전")]
        [SerializeField] private float combatSpeed = 3f;
        [Tooltip("이 거리보다 멀면 다가가고, 가까우면 멈춰서 쏜다")]
        [SerializeField] private float preferredRange = 7f;
        [Tooltip("처음 발견하고 쏘기까지 걸리는 시간(초)")]
        [SerializeField] private float reactionTime = 0.45f;
        [Tooltip("AI 부정확도. 무기 탄퍼짐에 더하는 각도")]
        [SerializeField] private float inaccuracy = 8f;
        [SerializeField] private int burstLength = 3;
        [SerializeField] private float burstPause = 0.6f;
        [SerializeField] private float calloutRadius = 8f;

        [Header("의심과 수색 (도착 후 두리번거리는 시간)")]
        [SerializeField] private float suspiciousDuration = 5f;
        [SerializeField] private float searchDuration = 8f;
        [Tooltip("확인하러 가는 길이 이 시간(초)보다 오래 걸리면 포기")]
        [SerializeField] private float travelTimeout = 15f;

        private EnemyMotor motor;
        private Perception perception;

        private State state = State.Patrol;
        private int waypointIndex;
        private float waypointResumeTime;
        private Vector3 checkPosition;
        private float stateEndTime;
        private bool engaged;
        private float canShootTime;
        private int shotsInBurst;
        private float burstResumeTime;
        private float lookBaseAngle;

        private void Awake()
        {
            motor = GetComponent<EnemyMotor>();
            perception = GetComponent<Perception>();
            if (gun == null) gun = GetComponentInChildren<Gun>();
        }

        private void OnEnable() => perception.Heard += OnHeard;
        private void OnDisable() => perception.Heard -= OnHeard;

        private void Update()
        {
            if (perception.Target != null) EnterCombat();

            switch (state)
            {
                case State.Patrol: Patrol(); break;
                case State.Suspicious: GoAndLook(patrolSpeed * 1.3f, suspiciousDuration, State.Patrol); break;
                case State.Combat: Combat(); break;
                case State.Search: GoAndLook(combatSpeed * 0.8f, searchDuration, State.Patrol); break;
            }
        }

        private void EnterCombat()
        {
            if (state == State.Combat) return;
            state = State.Combat;

            if (!engaged)
            {
                engaged = true;
                canShootTime = Time.time + reactionTime;
                NoiseSystem.Emit(transform.position, calloutRadius, gameObject);
            }
        }

        private void Patrol()
        {
            engaged = false;
            if (waypoints == null || waypoints.Length == 0)
            {
                motor.Stop();
                return;
            }
            if (Time.time < waypointResumeTime)
            {
                motor.Stop();
                return;
            }

            var wp = waypoints[waypointIndex];
            if (wp != null && !motor.MoveTowards(wp.position, patrolSpeed)) return;

            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            waypointResumeTime = Time.time + waypointWait;
        }

        private void Combat()
        {
            var target = perception.Target;
            if (target == null)
            {
                StartChecking(State.Search, perception.LastSeenPosition);
                return;
            }

            Vector3 toTarget = (target.transform.position - motor.Position).XZ();
            if (toTarget.magnitude > preferredRange) motor.MoveTowards(target.transform.position, combatSpeed, false);
            else motor.Stop();
            motor.FaceDirection(toTarget);

            if (gun == null || Time.time < canShootTime || Time.time < burstResumeTime) return;
            if (!motor.IsFacing(toTarget, 15f)) return;

            if (gun.Ammo <= 0)
            {
                gun.Reload();
                return;
            }

            if (gun.TryFire(toTarget, inaccuracy))
            {
                shotsInBurst++;
                if (shotsInBurst >= burstLength)
                {
                    shotsInBurst = 0;
                    burstResumeTime = Time.time + burstPause;
                }
            }
        }

        /// <summary>
        /// checkPosition 까지 가서 lookDuration 동안 두리번거린 뒤 nextState 로 돌아간다.
        /// 길이 막혀 travelTimeout 안에 못 가면 포기한다.
        /// </summary>
        private void GoAndLook(float speed, float lookDuration, State nextState)
        {
            if (Time.time >= stateEndTime)
            {
                state = nextState;
                return;
            }

            if (!motor.MoveTowards(checkPosition, speed)) return;

            if (float.IsNegativeInfinity(lookBaseAngle))
            {
                lookBaseAngle = transform.eulerAngles.y;
                stateEndTime = Time.time + lookDuration;
            }
            motor.FaceAngle(lookBaseAngle + Mathf.Sin(Time.time * 1.5f) * 70f);
        }

        private void StartChecking(State newState, Vector3 position)
        {
            state = newState;
            checkPosition = position;
            lookBaseAngle = Mathf.NegativeInfinity;
            stateEndTime = Time.time + travelTimeout;
        }

        private void OnHeard(Vector3 position)
        {
            if (state == State.Combat) return;
            StartChecking(state == State.Search ? State.Search : State.Suspicious, position);
        }
    }
}
