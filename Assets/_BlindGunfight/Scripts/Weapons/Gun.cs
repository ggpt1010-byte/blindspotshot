using System;
using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 히트스캔 총. 플레이어(PlayerShooter)와 무장 인간 AI(SoldierAI)가 같이 쓴다.
    /// 총알은 총구 높이에서 수평으로 나간다. 쏠 때마다 반동으로 조준이 조금씩 흔들리고,
    /// 맞은 위치에 따라 부위별 피해가 달라진다.
    /// </summary>
    public class Gun : MonoBehaviour
    {
        [SerializeField] private WeaponData weapon;
        [Tooltip("총구 위치. 비우면 캐릭터 중심 1.1m 높이에서 쏜다.")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] private BulletTracer tracerPrefab;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

        private Health ownerHealth;
        private GameObject owner;
        private int ammo;
        private float nextFireTime;
        private float reloadEndTime;
        private float recoilAngle;

        public WeaponData Weapon => weapon;
        public int Ammo => ammo;
        public bool IsReloading { get; private set; }
        /// <summary>지금 반동 때문에 조준이 틀어진 각도(도)</summary>
        public float RecoilAngle => recoilAngle;
        /// <summary>반동 배율. 플레이어가 조준 중이면 줄인다.</summary>
        public float RecoilMultiplier { get; set; } = 1f;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.1f;
        public LayerMask HitMask => hitMask;

        public event Action Fired;
        /// <summary>맞힌 대상, 부위, 실제 피해량</summary>
        public event Action<Health, HitZone, float> HitLanded;

        private void Awake()
        {
            ownerHealth = GetComponentInParent<Health>();
            owner = ownerHealth != null ? ownerHealth.gameObject : gameObject;
            ammo = weapon != null ? weapon.magazineSize : 0;
        }

        private void Update()
        {
            if (weapon != null) recoilAngle = Mathf.MoveTowards(recoilAngle, 0f, weapon.recoilRecovery * Time.deltaTime);

            if (IsReloading && Time.time >= reloadEndTime)
            {
                IsReloading = false;
                ammo = weapon.magazineSize;
            }
        }

        public void Equip(WeaponData newWeapon)
        {
            weapon = newWeapon;
            IsReloading = false;
            ammo = weapon != null ? weapon.magazineSize : 0;
        }

        /// <summary>무기 교체 때 남은 탄을 되돌려 놓는다</summary>
        public void SetAmmo(int value)
        {
            ammo = weapon != null ? Mathf.Clamp(value, 0, weapon.magazineSize) : 0;
        }

        public void Reload()
        {
            if (weapon == null || IsReloading || ammo >= weapon.magazineSize) return;
            IsReloading = true;
            reloadEndTime = Time.time + weapon.reloadTime;
        }

        /// <summary>반동이 반영된, 지금 실제로 총알이 나갈 방향</summary>
        public Vector3 AimedDirection(Vector3 direction)
        {
            direction = direction.XZ();
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward.XZ();
            return Flat.Rotate(direction.normalized, recoilAngle);
        }

        /// <param name="direction">쏘는 방향 (높이는 무시)</param>
        /// <param name="extraSpread">무기 기본 탄퍼짐에 더할 각도 (지향사격, AI 부정확도)</param>
        public bool TryFire(Vector3 direction, float extraSpread = 0f)
        {
            if (weapon == null || IsReloading || Time.time < nextFireTime) return false;
            if (ammo <= 0)
            {
                Reload();
                return false;
            }

            ammo--;
            nextFireTime = Time.time + 1f / weapon.fireRate;

            Vector3 origin = MuzzlePosition;
            direction = AimedDirection(direction);
            float totalSpread = weapon.spread + extraSpread;

            for (int i = 0; i < weapon.pellets; i++)
            {
                float angle = UnityEngine.Random.Range(-0.5f, 0.5f) * totalSpread;
                FireRay(origin, Flat.Rotate(direction, angle));
            }

            // 반동: 조준이 좌우로 무작위로 튄다. 연사할수록 쌓이고, 쉬면 천천히 돌아온다.
            float kick = UnityEngine.Random.Range(-1f, 1f) * weapon.recoilPerShot * RecoilMultiplier;
            recoilAngle = Mathf.Clamp(recoilAngle + kick, -weapon.maxRecoil, weapon.maxRecoil);

            NoiseSystem.Emit(origin, weapon.noiseRadius, owner);
            Fired?.Invoke();
            return true;
        }

        /// <summary>자기 자신을 빼고 가장 가까운 충돌을 찾는다. 레이저 조준선도 이걸 쓴다.</summary>
        public bool Trace(Vector3 origin, Vector3 direction, float range, out RaycastHit closest)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, HitBuffer, range, hitMask, QueryTriggerInteraction.Ignore);
            closest = default;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = HitBuffer[i];
                if (hit.collider.transform.IsChildOf(owner.transform)) continue;
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closest = hit;
                }
            }
            return closest.collider != null;
        }

        private void FireRay(Vector3 origin, Vector3 direction)
        {
            Vector3 end = origin + direction * weapon.range;
            if (Trace(origin, direction, weapon.range, out RaycastHit hit))
            {
                end = hit.point;
                var target = hit.collider.GetComponentInParent<Health>();
                bool sameSide = target != null && ownerHealth != null && !Factions.AreHostile(target.Faction, ownerHealth.Faction);
                if (target != null && !sameSide && !target.IsDead)
                {
                    HitZone zone = EvaluateZone(origin, direction, hit.collider);
                    float damage = weapon.damage * HitZones.DamageMultiplier(zone);
                    target.TakeDamage(damage, owner);
                    HitLanded?.Invoke(target, zone, damage);
                }
            }

            if (tracerPrefab != null)
            {
                var tracer = Instantiate(tracerPrefab);
                tracer.Show(origin, end);
            }
        }

        /// <summary>
        /// 탄의 직선이 맞은 캐릭터 중심(위에서 본 원)에서 얼마나 떨어져 지나갔는지로 부위를 정한다.
        /// </summary>
        private static HitZone EvaluateZone(Vector3 origin, Vector3 direction, Collider col)
        {
            Bounds bounds = col.bounds;
            float radius = Mathf.Max(0.01f, Mathf.Min(bounds.extents.x, bounds.extents.z));
            Vector3 toCenter = (bounds.center - origin).XZ();
            float offset = Mathf.Abs(toCenter.x * direction.z - toCenter.z * direction.x);
            return HitZones.Evaluate(offset / radius);
        }
    }
}
