using UnityEngine;

namespace BlindGunfight
{
    /// <summary>좌클릭 사격, R 재장전. 조준(우클릭)하지 않으면 탄이 더 퍼지고 반동도 크다.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private Gun gun;
        [Tooltip("조준하지 않고 쏠 때 더해지는 탄퍼짐(도)")]
        [SerializeField] private float hipFireSpread = 8f;
        [Tooltip("조준 중 반동 배율")]
        [SerializeField, Range(0f, 1f)] private float aimRecoilMultiplier = 0.6f;

        private PlayerController controller;

        public Gun Gun => gun;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            if (gun == null) gun = GetComponentInChildren<Gun>();
        }

        private void Update()
        {
            if (gun == null || gun.Weapon == null) return;

            if (GameInput.ReloadPressed) gun.Reload();
            gun.RecoilMultiplier = controller.IsAiming ? aimRecoilMultiplier : 1f;

            bool trigger = gun.Weapon.automatic ? GameInput.Fire : GameInput.FirePressed;
            if (trigger)
            {
                float spread = controller.IsAiming ? 0f : hipFireSpread;
                gun.TryFire(controller.AimDirection, spread);
            }
        }
    }
}
