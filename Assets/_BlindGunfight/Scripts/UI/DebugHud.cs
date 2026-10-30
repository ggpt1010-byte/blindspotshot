using System.Collections.Generic;
using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 프로토타입용 임시 HUD (블라인드스팟 배치를 따라함).
    /// 머리 위 이름표와 체력바, 오른쪽 아래 무기/탄약/체력, 가운데 아래 명중 문구, 오른쪽 키 안내.
    /// 정식 UI(uGUI 또는 UI Toolkit)가 나오기 전까지 쓴다.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerShooter playerShooter;
        [SerializeField] private float nameplateHeight = 2.2f;

        private PlayerLoadout loadout;

        private static readonly Color PlayerColor = new Color(0.3f, 0.9f, 0.6f);
        private static readonly Color EnemyColor = new Color(1f, 0.32f, 0.32f);
        private static readonly Color BarBack = new Color(0f, 0f, 0f, 0.6f);

        private GUIStyle smallStyle;
        private GUIStyle bigStyle;
        private GUIStyle centerStyle;
        private GUIStyle keyStyle;
        private string lastHitText;
        private float lastHitTime = float.NegativeInfinity;
        private readonly List<(Health health, Vector3 screen)> plates = new List<(Health, Vector3)>();
        private readonly List<Rect> placedPlates = new List<Rect>();

        private const float DamageMarkDuration = 1.2f;
        private readonly List<(Vector3 from, float time)> damageMarks = new List<(Vector3, float)>();

        private void Start()
        {
            if (playerShooter != null && playerShooter.Gun != null) playerShooter.Gun.HitLanded += OnHitLanded;
            if (playerShooter != null) loadout = playerShooter.GetComponent<PlayerLoadout>();
            if (playerHealth != null) playerHealth.Damaged += OnPlayerDamaged;
        }

        private void OnDestroy()
        {
            if (playerShooter != null && playerShooter.Gun != null) playerShooter.Gun.HitLanded -= OnHitLanded;
            if (playerHealth != null) playerHealth.Damaged -= OnPlayerDamaged;
        }

        private void OnPlayerDamaged(float amount, GameObject source)
        {
            if (source == null) return;
            if (damageMarks.Count >= 4) damageMarks.RemoveAt(0);
            damageMarks.Add((source.transform.position, Time.time));
        }

        private void OnHitLanded(Health target, HitZone zone, float damage)
        {
            string label = zone switch
            {
                HitZone.Head => "HEADSHOT",
                HitZone.Body => "BODY",
                HitZone.Limb => "LIMB",
                _ => "GRAZE",
            };
            lastHitText = target.IsDead ? $"KILL  +20 PT   ({label})" : $"{label}  {Mathf.RoundToInt(damage)}";
            lastHitTime = Time.time;
        }

        private void EnsureStyles()
        {
            if (smallStyle != null) return;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Overflow };
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            centerStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            keyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleRight };
            smallStyle.normal.textColor = Color.white;
            bigStyle.normal.textColor = Color.white;
            keyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawNameplates();
            DrawHitText();
            DrawDamageMarks();
            DrawWeaponPanel();
            DrawKeyGuide();
        }

        // 맞은 방향 표시: 플레이어 주변 원 위에, 쏜 쪽을 향한 빨간 막대가 잠깐 뜬다
        private void DrawDamageMarks()
        {
            var cam = Camera.main;
            if (cam == null || playerHealth == null) return;

            Vector3 p = cam.WorldToScreenPoint(playerHealth.transform.position + Vector3.up);
            var center = new Vector2(p.x, Screen.height - p.y);
            float radius = Mathf.Min(Screen.width, Screen.height) * 0.22f;

            for (int i = damageMarks.Count - 1; i >= 0; i--)
            {
                float age = Time.time - damageMarks[i].time;
                if (age > DamageMarkDuration)
                {
                    damageMarks.RemoveAt(i);
                    continue;
                }

                Vector3 s = cam.WorldToScreenPoint(damageMarks[i].from + Vector3.up);
                Vector2 dir = new Vector2(s.x, Screen.height - s.y) - center;
                if (s.z < 0f) dir = -dir;
                if (dir.sqrMagnitude < 1f) continue;
                dir.Normalize();

                Vector2 pos = center + dir * radius;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
                Matrix4x4 saved = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, pos);
                GUI.color = new Color(1f, 0.15f, 0.15f, 0.85f * (1f - age / DamageMarkDuration));
                GUI.DrawTexture(new Rect(pos.x - 40f, pos.y - 5f, 80f, 10f), Texture2D.whiteTexture);
                GUI.matrix = saved;
                GUI.color = Color.white;
            }
        }

        private void DrawNameplates()
        {
            var cam = Camera.main;
            if (cam == null) return;

            plates.Clear();
            foreach (var h in Health.All)
            {
                if (h == null || h.IsDead) continue;
                if (h.TryGetComponent(out HiddenWhenUnseen hidden) && !hidden.IsShown) continue;

                Vector3 screen = cam.WorldToScreenPoint(h.transform.position + Vector3.up * nameplateHeight);
                if (screen.z <= 0f) continue;
                plates.Add((h, screen));
            }

            // 화면 아래쪽 이름표부터 놓고, 이미 놓인 이름표와 겹치면 위로 올린다
            plates.Sort((a, b) =>
            {
                // 플레이어 이름표는 항상 먼저 자리를 잡는다
                if (a.health == playerHealth) return -1;
                if (b.health == playerHealth) return 1;
                return a.screen.y.CompareTo(b.screen.y);
            });
            placedPlates.Clear();
            foreach (var (h, screen) in plates)
            {
                float x = screen.x;
                float y = Screen.height - screen.y;
                var rect = new Rect(x - 90f, y - 24f, 180f, 26f);
                for (int tries = 0; tries < 6 && placedPlates.Exists(r => r.Overlaps(rect)); tries++) rect.y -= 26f;
                placedPlates.Add(rect);

                bool isPlayer = h == playerHealth;
                Color color = isPlayer ? PlayerColor : EnemyColor;

                smallStyle.normal.textColor = color;
                GUI.Label(new Rect(rect.x, rect.y, rect.width, 18f), h.DisplayName, smallStyle);
                DrawBar(new Rect(rect.center.x - 40f, rect.y + 20f, 80f, 6f), h.Current / h.Max, color);
            }
        }

        private void DrawHitText()
        {
            float age = Time.time - lastHitTime;
            if (age > 1f) return;

            GUI.color = new Color(1f, 1f, 1f, 1f - age);
            GUI.Box(new Rect(Screen.width * 0.5f - 130f, Screen.height - 140f, 260f, 34f), lastHitText, centerStyle);
            GUI.color = Color.white;
        }

        private void DrawWeaponPanel()
        {
            float right = Screen.width - 30f;
            float bottom = Screen.height - 30f;

            if (playerHealth != null)
            {
                GUI.Label(new Rect(right - 360f, bottom - 120f, 40f, 20f), Mathf.CeilToInt(playerHealth.Current).ToString(), keyStyle);
                DrawBar(new Rect(right - 310f, bottom - 115f, 310f, 10f), playerHealth.Current / playerHealth.Max, PlayerColor);
            }

            var gun = playerShooter != null ? playerShooter.Gun : null;
            if (gun == null || gun.Weapon == null) return;

            if (loadout == null)
            {
                DrawSlot(new Rect(right - 310f, bottom - 90f, 310f, 70f), "1", gun.Weapon.displayName, AmmoText(gun, gun.Ammo, gun.Weapon), true);
                return;
            }

            // 블라인드스팟처럼 오른쪽 아래에 무기 슬롯을 나란히 놓는다. 활성 슬롯은 넓고 초록 테두리.
            float x = right - 310f;
            for (int i = 0; i < PlayerLoadout.SlotCount; i++)
            {
                var weapon = loadout.GetWeapon(i);
                if (weapon == null) continue;
                bool active = i == loadout.ActiveSlot;
                float width = active ? 190f : 110f;
                string ammo = active ? AmmoText(gun, gun.Ammo, weapon) : $"{loadout.GetAmmo(i)} / {weapon.magazineSize}";
                DrawSlot(new Rect(x, bottom - 90f, width, 70f), (i + 1).ToString(), weapon.displayName, ammo, active);
                x += width + 10f;
            }
        }

        private static string AmmoText(Gun gun, int ammo, WeaponData weapon) =>
            gun.IsReloading ? "..." : $"{ammo} / {weapon.magazineSize}";

        private void DrawSlot(Rect rect, string key, string weaponName, string ammo, bool active)
        {
            GUI.color = active ? new Color(0.3f, 0.9f, 0.6f, 0.9f) : new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            smallStyle.normal.textColor = active ? PlayerColor : new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 18f), weaponName, smallStyle);
            bigStyle.fontSize = active ? 30 : 20;
            GUI.Label(new Rect(rect.x + 6f, rect.y + 20f, rect.width - 14f, rect.height - 22f), ammo, bigStyle);
            GUI.Label(new Rect(rect.center.x - 10f, rect.yMax + 2f, 20f, 18f), key, smallStyle);
        }

        private void DrawKeyGuide()
        {
            string[] lines =
            {
                "Aim  RMB", "Fire  LMB", "Walk  WASD", "Run  Shift", "Crouch  C", "Low aim  Ctrl",
                "Reload  R", "Knife  F", "Spot  Q", "Weapon  1 / 2", "Lobby  Esc",
            };
            // 오른쪽 위에 둔다 (오른쪽 아래 체력바/무기 슬롯을 가리지 않게)
            float y = 70f;
            foreach (string line in lines)
            {
                GUI.Label(new Rect(Screen.width - 210f, y, 190f, 18f), line, keyStyle);
                y += 18f;
            }
        }

        private static void DrawBar(Rect rect, float ratio, Color color)
        {
            GUI.color = BarBack;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
