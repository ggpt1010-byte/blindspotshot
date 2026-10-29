using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 총구에서 나가는 얇은 조준 레이저선. 반동이 반영된 실제 탄 방향을 보여준다.
    /// 조준(우클릭) 중에는 더 밝아진다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserSight : MonoBehaviour
    {
        [SerializeField] private Gun gun;
        [SerializeField] private PlayerController controller;
        [SerializeField] private float maxLength = 20f;
        [SerializeField] private Color idleColor = new Color(0.4f, 1f, 0.6f, 0.25f);
        [SerializeField] private Color aimColor = new Color(0.4f, 1f, 0.6f, 0.8f);

        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
        }

        private void LateUpdate()
        {
            if (gun == null || controller == null)
            {
                line.enabled = false;
                return;
            }

            Vector3 origin = gun.MuzzlePosition;
            Vector3 direction = gun.AimedDirection(controller.AimDirection);
            float length = gun.Weapon != null ? Mathf.Min(maxLength, gun.Weapon.range) : maxLength;
            Vector3 end = gun.Trace(origin, direction, length, out RaycastHit hit) ? hit.point : origin + direction * length;

            Color c = controller.IsAiming ? aimColor : idleColor;
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, c.a * 0.3f);
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            line.enabled = true;
        }
    }
}
