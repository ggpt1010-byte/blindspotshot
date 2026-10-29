using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 로비에서 고른 요원의 장비로 시작한다. 1 = 주무기, 2 = 보조무기. Esc 를 누르면 로비로 돌아간다.
    /// 로비를 거치지 않고 미션 씬을 바로 실행하면 defaultAgent 를 쓴다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerShooter))]
    public class PlayerLoadout : MonoBehaviour
    {
        public const int SlotCount = 2;

        [SerializeField] private AgentData defaultAgent;
        [SerializeField] private Gun gun;

        private readonly WeaponData[] slots = new WeaponData[SlotCount];
        private readonly int[] storedAmmo = new int[SlotCount];

        public AgentData Agent { get; private set; }
        public int ActiveSlot { get; private set; }

        private void Awake()
        {
            if (gun == null) gun = GetComponentInChildren<Gun>();
            Agent = GameSession.SelectedAgent != null ? GameSession.SelectedAgent : defaultAgent;
            if (Agent == null || gun == null) return;

            slots[0] = Agent.primaryWeapon;
            slots[1] = Agent.secondaryWeapon;
            for (int i = 0; i < SlotCount; i++) storedAmmo[i] = slots[i] != null ? slots[i].magazineSize : 0;

            if (TryGetComponent(out Health health)) health.Configure(Agent.agentName, Agent.maxHealth);
            if (TryGetComponent(out PlayerController controller)) controller.SpeedMultiplier = Agent.moveSpeedMultiplier;

            ActiveSlot = slots[0] != null ? 0 : 1;
            gun.Equip(slots[ActiveSlot]);
        }

        private void Update()
        {
            if (GameInput.Slot1Pressed) Switch(0);
            if (GameInput.Slot2Pressed) Switch(1);
            if (GameInput.BackPressed) GameSession.ReturnToLobby();
        }

        public WeaponData GetWeapon(int slot) => slot >= 0 && slot < SlotCount ? slots[slot] : null;

        public int GetAmmo(int slot) => slot == ActiveSlot && gun != null ? gun.Ammo : storedAmmo[slot];

        public void Switch(int slot)
        {
            if (gun == null || slot == ActiveSlot || GetWeapon(slot) == null) return;

            storedAmmo[ActiveSlot] = gun.Ammo;
            ActiveSlot = slot;
            gun.Equip(slots[slot]);
            gun.SetAmmo(storedAmmo[slot]);
        }
    }
}
