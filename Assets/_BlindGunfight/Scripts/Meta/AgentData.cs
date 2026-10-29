using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 플레이할 수 있는 구조팀 요원 한 명. 요원마다 고유 주무기, 보조무기, 도구를 가진다.
    /// </summary>
    [CreateAssetMenu(menuName = "BlindGunfight/Agent", fileName = "Agent")]
    public class AgentData : ScriptableObject
    {
        [Header("소개")]
        public string agentName = "Ethan Cole";
        [Tooltip("역할 (돌격, 정찰, 돌파, 저격 등)")]
        public string role = "돌격";
        [TextArea(3, 6)] public string description = "";
        [Tooltip("요원 목록 칸과 큰 그림 배경에 쓰는 색")]
        public Color themeColor = new Color(0.9f, 0.75f, 0.2f);
        [Tooltip("요원 그림. 비우면 이름 머리글자로 대신 보여준다")]
        public Sprite portrait;
        [Tooltip("스토리 진행 전에는 잠겨 있다")]
        public bool locked;

        [Header("능력치")]
        [Min(1f)] public float maxHealth = 100f;
        [Range(0.8f, 1.2f)] public float moveSpeedMultiplier = 1f;

        [Header("장비")]
        public WeaponData primaryWeapon;
        public WeaponData secondaryWeapon;
        [Tooltip("요원 전용 도구 (아직 화면 표시만, 기능은 다음 단계)")]
        public string signatureTool = "";
        public string tool = "";

        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(agentName)) return "?";
                string[] parts = agentName.Split(' ');
                string result = "";
                foreach (string p in parts)
                {
                    if (p.Length > 0) result += char.ToUpperInvariant(p[0]);
                }
                return result;
            }
        }
    }
}
