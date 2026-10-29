using UnityEngine;
using UnityEngine.AI;

namespace BlindGunfight
{
    /// <summary>
    /// 적의 이동(NavMesh 길찾기)과 회전. 회전은 AI가 직접 정한다(이동하면서 다른 곳을 볼 수 있게).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMotor : MonoBehaviour
    {
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float acceleration = 20f;

        private NavMeshAgent agent;
        private float desiredYaw;
        private Vector3 lastDestination = new Vector3(float.MaxValue, 0f, 0f);

        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.acceleration = acceleration;
            agent.angularSpeed = 0f;
            agent.autoBraking = true;
            desiredYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            // NavMesh 는 RuntimeNavMesh 가 Awake 에서 만든다. 그 뒤에 에이전트를 켠다.
            agent.enabled = true;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        private void Update()
        {
            float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, desiredYaw, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void OnDisable()
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        }

        /// <returns>목표에 도착했으면 true (길이 막혀 더 못 가도 true)</returns>
        public bool MoveTowards(Vector3 target, float speed, bool faceMovement = true, float stopDistance = 0.3f)
        {
            if (Flat.Distance(target, transform.position) <= stopDistance)
            {
                Stop();
                return true;
            }
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;

            agent.speed = speed;
            agent.stoppingDistance = stopDistance;
            agent.isStopped = false;
            if ((target - lastDestination).sqrMagnitude > 0.04f)
            {
                agent.SetDestination(target);
                lastDestination = target;
            }

            if (faceMovement && agent.desiredVelocity.sqrMagnitude > 0.01f) FaceDirection(agent.desiredVelocity);

            bool arrived = !agent.pathPending && agent.remainingDistance <= stopDistance + 0.05f;
            if (arrived) Stop();
            return arrived;
        }

        public void Stop()
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
            lastDestination = new Vector3(float.MaxValue, 0f, 0f);
        }

        public void FaceDirection(Vector3 direction)
        {
            direction = direction.XZ();
            if (direction.sqrMagnitude < 0.0001f) return;
            desiredYaw = Flat.Yaw(direction);
        }

        public void FaceAngle(float yawDegrees)
        {
            desiredYaw = yawDegrees;
        }

        /// <summary>지금 보는 방향이 direction 과 몇 도 이내인지</summary>
        public bool IsFacing(Vector3 direction, float tolerance)
        {
            return Vector3.Angle(transform.forward.XZ(), direction.XZ()) <= tolerance;
        }
    }
}
