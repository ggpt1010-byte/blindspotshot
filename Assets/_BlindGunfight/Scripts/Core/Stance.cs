using System.Collections.Generic;
using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 서기 / 앉기 자세. 앉으면 몸 그림을 낮추고, 명중 부위 판정이 바뀐다 (HitZones.Evaluate 참고).
    /// 앉은 적은 Ctrl(낮게 쏘기)로 쏴야 머리에 맞는다.
    /// </summary>
    public class Stance : MonoBehaviour
    {
        [Tooltip("앉았을 때 몸 그림 높이 배율")]
        [SerializeField, Range(0.4f, 1f)] private float crouchHeightScale = 0.65f;

        private readonly List<(Transform part, Vector3 position, Vector3 scale)> parts = new List<(Transform, Vector3, Vector3)>();

        public bool Crouching { get; private set; }

        private void Awake()
        {
            // 몸 그림(메시)이 달린 바로 아래 자식만 낮춘다. 조명, 총구, 마커는 그대로 둔다.
            foreach (Transform child in transform)
            {
                if (child.GetComponentInChildren<MeshRenderer>(true) == null &&
                    child.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) continue;
                if (child.GetComponentInChildren<Light>(true) != null) continue;
                parts.Add((child, child.localPosition, child.localScale));
            }
        }

        public void SetCrouching(bool value)
        {
            if (Crouching == value) return;
            Crouching = value;

            float k = value ? crouchHeightScale : 1f;
            foreach (var (part, position, scale) in parts)
            {
                if (part == null) continue;
                part.localPosition = new Vector3(position.x, position.y * k, position.z);
                part.localScale = new Vector3(scale.x, scale.y * k, scale.z);
            }
        }
    }
}
