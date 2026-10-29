using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 좀비: 배회 → 소리 추적 → 추격 → 공격.
    /// 속도와 비명 설정만 바꿔서 배회자, 주자, 비명자를 만든다.
    /// </summary>
    [RequireComponent(typeof(EnemyMotor), typeof(Perception), typeof(Health))]
    public class ZombieAI : MonoBehaviour
    {
        private enum State { Wander, Investigate, Chase }

        [Header("속도 (m/s)")]
        [SerializeField] private float wanderSpeed = 0.8f;
        [SerializeField] private float chaseSpeed = 2.5f;

        [Header("배회")]
        [SerializeField] private float wanderRadius = 4f;
        [SerializeField] private Vector2 wanderPauseRange = new Vector2(1f, 3f);

        [Header("공격")]
        [SerializeField] private float attackRange = 0.8f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 1.2f;

        [Header("추격")]
        [Tooltip("놓친 뒤 마지막 목격 위치까지 쫓아가는 시간(초)")]
        [SerializeField] private float giveUpTime = 5f;

        [Header("비명자")]
        [SerializeField] private bool screamsOnSight = false;
        [SerializeField] private float screamRadius = 15f;

        private EnemyMotor motor;
        private Perception perception;

        private State state = State.Wander;
        private Vector3 home;
        private Vector3 wanderTarget;
        private float wanderResumeTime;
        private Vector3 investigatePosition;
        private float nextAttackTime;
        private bool hasScreamed;

        private void Awake()
        {
            motor = GetComponent<EnemyMotor>();
            perception = GetComponent<Perception>();
            home = transform.position;
            wanderTarget = home;
        }

        private void OnEnable() => perception.Heard += OnHeard;
        private void OnDisable() => perception.Heard -= OnHeard;

        private void Update()
        {
            if (perception.Target != null)
            {
                if (screamsOnSight && !hasScreamed)
                {
                    hasScreamed = true;
                    NoiseSystem.Emit(transform.position, screamRadius, gameObject);
                }
                state = State.Chase;
            }

            switch (state)
            {
                case State.Wander: Wander(); break;
                case State.Investigate: Investigate(); break;
                case State.Chase: Chase(); break;
            }
        }

        private void Wander()
        {
            if (Time.time < wanderResumeTime)
            {
                motor.Stop();
                return;
            }

            if (motor.MoveTowards(wanderTarget, wanderSpeed))
            {
                wanderTarget = home + RandomFlatOffset(wanderRadius);
                wanderResumeTime = Time.time + Random.Range(wanderPauseRange.x, wanderPauseRange.y);
            }
        }

        private void Investigate()
        {
            if (motor.MoveTowards(investigatePosition, chaseSpeed * 0.8f))
            {
                // 소리 난 곳에 도착하면 그 근처를 새 배회 중심으로 삼는다.
                home = investigatePosition;
                state = State.Wander;
                wanderResumeTime = Time.time + 1.5f;
            }
        }

        private void Chase()
        {
            var target = perception.Target;
            if (target == null)
            {
                bool lostTooLong = Time.time - perception.LastSeenTime > giveUpTime;
                if (lostTooLong || motor.MoveTowards(perception.LastSeenPosition, chaseSpeed))
                {
                    home = transform.position;
                    state = State.Wander;
                }
                return;
            }

            Vector3 toTarget = (target.transform.position - motor.Position).XZ();
            if (toTarget.magnitude > attackRange)
            {
                motor.MoveTowards(target.transform.position, chaseSpeed);
                return;
            }

            motor.Stop();
            motor.FaceDirection(toTarget);
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                target.TakeDamage(attackDamage, gameObject);
            }
        }

        private static Vector3 RandomFlatOffset(float radius)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            return new Vector3(r.x, 0f, r.y);
        }

        private void OnHeard(Vector3 position)
        {
            if (state == State.Chase && perception.Target != null) return;
            investigatePosition = position;
            state = State.Investigate;
        }
    }
}
