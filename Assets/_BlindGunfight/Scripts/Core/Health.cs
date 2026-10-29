using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlindGunfight
{
    public enum Faction
    {
        Player,
        Zombie,
        Mercenary, // 회색늑대
        Militia,   // 자경단
    }

    public static class Factions
    {
        // 지금은 서로 다른 세력이면 모두 적대한다. 자경단 분기는 나중에 여기서 바꾼다.
        public static bool AreHostile(Faction a, Faction b) => a != b;
    }

    public class Health : MonoBehaviour
    {
        /// <summary>씬에 있는 모든 캐릭터 (이름표 표시 등에 쓴다)</summary>
        public static readonly List<Health> All = new List<Health>();

        [SerializeField] private Faction faction = Faction.Player;
        [Tooltip("머리 위 이름표에 보일 이름. 비우면 오브젝트 이름")]
        [SerializeField] private string displayName = "";
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [Tooltip("받는 피해 감소 비율 (방탄복 0.3 = 30% 감소)")]
        [SerializeField, Range(0f, 0.9f)] private float damageReduction = 0f;

        public Faction Faction => faction;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>받은 피해량, 공격한 오브젝트(없으면 null)</summary>
        public event Action<float, GameObject> Damaged;
        public event Action Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        /// <summary>요원 데이터로 이름과 최대 체력을 정한다 (게임 시작 때 PlayerLoadout 이 부른다)</summary>
        public void Configure(string newDisplayName, float newMaxHealth)
        {
            displayName = newDisplayName;
            maxHealth = Mathf.Max(1f, newMaxHealth);
            Current = maxHealth;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public void TakeDamage(float amount, GameObject source)
        {
            if (IsDead || amount <= 0f) return;

            amount *= 1f - damageReduction;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount, source);
            if (Current <= 0f) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }
    }
}
