using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 플레이어와 적이 똑같이 쓰는 시야 판정.
    /// 부채꼴(시야각, 거리) 안이거나 아주 가까운 원 안이고, 눈높이에서 벽에 가리지 않으면 보인다.
    /// 눈높이보다 낮은 상자는 시야를 막지 않는다.
    /// </summary>
    public static class VisionMath
    {
        public const float EyeHeight = 1.4f;

        public static bool CanSee(Vector3 origin, Vector3 forward, float viewAngle, float viewDistance,
                                  float closeRadius, Vector3 target, LayerMask obstacleMask)
        {
            Vector3 toTarget = (target - origin).XZ();
            float distance = toTarget.magnitude;
            if (distance > Mathf.Max(viewDistance, closeRadius)) return false;

            bool insideCloseCircle = distance <= closeRadius;
            if (!insideCloseCircle && Vector3.Angle(forward.XZ(), toTarget) > viewAngle * 0.5f) return false;
            if (distance < 0.001f) return true;

            Vector3 eye = new Vector3(origin.x, EyeHeight, origin.z);
            Vector3 targetEye = new Vector3(target.x, EyeHeight, target.z);
            return !Physics.Linecast(eye, targetEye, obstacleMask, QueryTriggerInteraction.Ignore);
        }

#if UNITY_EDITOR
        public static void DrawConeGizmo(Vector3 origin, Vector3 forward, float viewAngle, float viewDistance, float closeRadius)
        {
            forward = forward.XZ().normalized;
            Vector3 left = Flat.Rotate(forward, -viewAngle * 0.5f) * viewDistance;
            Vector3 right = Flat.Rotate(forward, viewAngle * 0.5f) * viewDistance;
            Gizmos.DrawLine(origin, origin + left);
            Gizmos.DrawLine(origin, origin + right);
            UnityEditor.Handles.color = Gizmos.color;
            UnityEditor.Handles.DrawWireArc(origin, Vector3.up, left, viewAngle, viewDistance);
            UnityEditor.Handles.DrawWireDisc(origin, Vector3.up, closeRadius);
        }
#endif
    }
}
