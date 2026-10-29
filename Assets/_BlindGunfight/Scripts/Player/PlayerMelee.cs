using UnityEngine;

namespace BlindGunfight
{
    /// <summary>F 키 전술 나이프. 소리가 나지 않고, 적의 등 뒤에서 찌르면 즉사.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMelee : MonoBehaviour
    {
        [SerializeField] private float damage = 50f;
        [SerializeField] private float reach = 1f;
        [SerializeField] private float hitRadius = 0.6f;
        [SerializeField] private float cooldown = 0.6f;
        [Tooltip("적이 보는 방향과 이 각도 이내로 뒤에서 찌르면 즉사")]
        [SerializeField] private float backstabAngle = 60f;

        private static readonly Collider[] Buffer = new Collider[8];

        private PlayerController controller;
        private Health self;
        private float nextAttackTime;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            self = GetComponent<Health>();
        }

        private void Update()
        {
            if (!GameInput.MeleePressed || Time.time < nextAttackTime) return;
            nextAttackTime = Time.time + cooldown;

            Vector3 center = transform.position.XZ() + controller.AimDirection * reach + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(center, hitRadius, Buffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var target = Buffer[i].GetComponentInParent<Health>();
                if (target == null || target == self || target.IsDead) continue;
                if (self != null && !Factions.AreHostile(self.Faction, target.Faction)) continue;

                // 적의 앞쪽과 내가 찌르는 방향이 같으면 등 뒤에서 찌른 것이다.
                bool backstab = Vector3.Angle(target.transform.forward.XZ(), controller.AimDirection) <= backstabAngle;
                target.TakeDamage(backstab ? target.Max * 10f : damage, gameObject);
                break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * reach + Vector3.up, hitRadius);
        }
#endif
    }
}
