using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 만화체 모델을 조금 더 사람 비율로 보이게 한다 (머리 축소, 허벅지 연장).
    /// 애니메이션이 매 프레임 뼈를 덮어쓰므로 Animator 다음(LateUpdate)에 다시 적용한다.
    /// 모델(Animator 가 달린 오브젝트)에 붙인다.
    /// </summary>
    public class BodyProportions : MonoBehaviour
    {
        [Tooltip("머리 크기 배율 (R2 = 0.7)")]
        [SerializeField, Range(0.4f, 1.2f)] private float headScale = 0.7f;
        [Tooltip("허벅지 길이 배율 (R2 = 1.1)")]
        [SerializeField, Range(0.8f, 1.4f)] private float legLength = 1.1f;

        private Transform head;
        private Transform[] upperLegs = new Transform[0];
        private Vector3 headBase;
        private Vector3[] legBase = new Vector3[0];

        private void Awake()
        {
            var bones = GetComponentsInChildren<Transform>(true);
            head = System.Array.Find(bones, b => b.name == "Head");
            upperLegs = System.Array.FindAll(bones, b => b.name.StartsWith("UpperLeg"));
            if (head != null) headBase = head.localScale;
            legBase = System.Array.ConvertAll(upperLegs, b => b.localScale);
        }

        private void LateUpdate()
        {
            if (head != null) head.localScale = headBase * headScale;
            for (int i = 0; i < upperLegs.Length; i++)
            {
                var s = legBase[i];
                upperLegs[i].localScale = new Vector3(s.x, s.y * legLength, s.z);
            }
        }
    }
}
