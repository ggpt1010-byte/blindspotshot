using UnityEngine;

namespace BlindGunfight
{
    [CreateAssetMenu(menuName = "BlindGunfight/Weapon", fileName = "Weapon")]
    public class WeaponData : ScriptableObject
    {
        public string displayName = "Pistol";

        [Header("피해")]
        [Min(0f)] public float damage = 30f;
        [Tooltip("한 번 쏠 때 나가는 탄 수 (산탄총 8)")]
        [Min(1)] public int pellets = 1;
        [Tooltip("탄퍼짐 전체 각도(도)")]
        [Min(0f)] public float spread = 4f;
        [Min(0.1f)] public float range = 20f;

        [Header("연사")]
        [Tooltip("초당 발사 수")]
        [Min(0.1f)] public float fireRate = 4f;
        [Tooltip("켜면 누르고 있는 동안 연사")]
        public bool automatic = false;

        [Header("탄창")]
        [Min(1)] public int magazineSize = 12;
        [Min(0f)] public float reloadTime = 1.5f;

        [Header("반동")]
        [Tooltip("한 발 쏠 때 조준이 튀는 최대 각도(도)")]
        [Min(0f)] public float recoilPerShot = 2f;
        [Tooltip("반동으로 틀어질 수 있는 최대 각도(도)")]
        [Min(0f)] public float maxRecoil = 10f;
        [Tooltip("초당 회복되는 반동 각도(도)")]
        [Min(0f)] public float recoilRecovery = 12f;

        [Header("소음")]
        [Tooltip("총소리가 들리는 반경(m). 소음기 6, 일반 20")]
        [Min(0f)] public float noiseRadius = 20f;
    }
}
