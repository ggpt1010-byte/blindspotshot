using UnityEngine;

namespace BlindGunfight
{
    /// <summary>쿼터뷰 게임은 바닥(XZ 평면) 위에서 돌아간다. 높이(Y)를 뺀 계산 도우미.</summary>
    public static class Flat
    {
        public static Vector3 XZ(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        public static float Distance(Vector3 a, Vector3 b) => (a - b).XZ().magnitude;

        public static Vector3 Direction(Vector3 from, Vector3 to)
        {
            Vector3 d = (to - from).XZ();
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.zero;
        }

        /// <summary>Y축 기준으로 degrees 만큼 돌린 방향</summary>
        public static Vector3 Rotate(Vector3 direction, float degrees) => Quaternion.Euler(0f, degrees, 0f) * direction;

        public static float Yaw(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
}
