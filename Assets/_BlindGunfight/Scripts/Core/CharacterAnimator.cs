using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 캐릭터 모델(Animator)에 이동 속도와 죽음을 넘기고, 들고 있는 총만 보이게 한다.
    /// 모델은 PrototypeSceneBuilder 가 캐릭터의 자식으로 만든다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class CharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int DeadId = Animator.StringToHash("Dead");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Animator animator;
        [Tooltip("죽는 애니메이션이 있으면 DeathHandler 가 몸을 옆으로 눕히지 않는다")]
        [SerializeField] private bool hasDeathAnimation = true;
        [Tooltip("모델 색을 이 색으로 곱한다 (흰색 = 원래 색). Greywolf 구분용")]
        [SerializeField] private Color tint = Color.white;
        [Tooltip("모델에 붙은 무기 오브젝트들. 들고 있는 총 이름과 같은 것만 켠다")]
        [SerializeField] private GameObject[] weaponModels = new GameObject[0];
        [SerializeField] private float speedSmoothing = 10f;

        private Health health;
        private Gun gun;
        private WeaponData shownWeapon;
        private Vector3 lastPosition;
        private float speed;

        public bool HasDeathAnimation => hasDeathAnimation && animator != null;

        private void Awake()
        {
            health = GetComponent<Health>();
            gun = GetComponent<Gun>();
            lastPosition = transform.position;
            if (animator != null) animator.applyRootMotion = false;
            ApplyTint();
            UpdateWeaponModel();
        }

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void Update()
        {
            if (animator == null || health.IsDead) return;

            // Rigidbody 든 NavMeshAgent 든 실제로 움직인 거리로 속도를 잰다
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float measured = Flat.Distance(transform.position, lastPosition) / dt;
            lastPosition = transform.position;
            speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-speedSmoothing * dt));
            animator.SetFloat(SpeedId, speed);

            if (gun != null && gun.Weapon != shownWeapon) UpdateWeaponModel();
        }

        private void OnDied()
        {
            if (animator != null) animator.SetTrigger(DeadId);
        }

        private void UpdateWeaponModel()
        {
            shownWeapon = gun != null ? gun.Weapon : null;
            string wanted = ModelWeaponName(shownWeapon);
            foreach (var w in weaponModels)
            {
                if (w != null) w.SetActive(w.name == wanted);
            }
        }

        /// <summary>무기 데이터 이름 → 모델에 들어 있는 무기 오브젝트 이름</summary>
        public static string ModelWeaponName(WeaponData weapon)
        {
            if (weapon == null) return "";
            string n = weapon.displayName;
            if (n.StartsWith("SMG")) return "SMG";
            if (n == "DMR") return "Rifle";
            return n; // Rifle, Pistol, Shotgun
        }

        private void ApplyTint()
        {
            if (tint == Color.white || animator == null) return;
            var block = new MaterialPropertyBlock();
            foreach (var r in animator.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterial == null || !r.sharedMaterial.HasProperty(BaseColorId)) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, r.sharedMaterial.GetColor(BaseColorId) * tint);
                r.SetPropertyBlock(block);
            }
        }
    }
}
