using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace BlindGunfight
{
    /// <summary>
    /// 사람 없이 게임을 한 판 돌리며 스크린샷과 영상용 프레임을 남기는 자동 촬영 도구.
    /// 빌드한 게임을 "-autocapture" 인자로 실행할 때만 켜진다. (예: BlindGunfight.exe -autocapture)
    /// 순서: 로비 → 요원 탭 → 출격 → 가까운 적에게 다가가 조준·사격 → 종료.
    /// 결과는 Capture 폴더(-captureDir 로 바꿀 수 있음)에 stills/*.png, frames/*.jpg, log.txt 로 남는다.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class AutoCapture : MonoBehaviour
    {
        private const int FrameRate = 20;
        private const float RecordSeconds = 45f;
        private const float ReactionTime = 0.4f; // 적을 처음 본 뒤 쏘기까지 (사람 반응 속도)
        private const float AimError = 0.5f;     // 조준이 흔들리는 반경 (m)
        private const float StillInterval = 4f;

        private readonly SimulatedInput input = new SimulatedInput();
        private readonly StringBuilder log = new StringBuilder();
        private NavMeshPath path;

        private string outputDir;
        private bool recording;
        private int frameIndex;
        private int stillIndex;

        private Transform player;
        private Health playerHealth;
        private PlayerShooter shooter;
        private Health target;
        private Health lastSpotted;
        private Health seenTarget;
        private float seenTime;
        private Vector3 aimOffset;
        private float nextAimOffsetTime;
        private float nextPathTime;
        private float nextTriggerTime;
        private Vector3 waypoint;
        private bool pendingStill;
        private string pendingStillName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-autocapture") < 0) return;

            var go = new GameObject("AutoCapture");
            DontDestroyOnLoad(go);
            var capture = go.AddComponent<AutoCapture>();
            int dirIndex = Array.IndexOf(args, "-captureDir");
            capture.outputDir = dirIndex >= 0 && dirIndex + 1 < args.Length
                ? args[dirIndex + 1]
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capture"));
        }

        private void Awake()
        {
            // NavMeshPath 는 생성자(필드 초기화)에서 만들 수 없다
            path = new NavMeshPath();
            Application.logMessageReceived += OnLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            GameInput.Simulated = null;
            Time.captureFramerate = 0;
        }

        private void Start()
        {
            // outputDir 는 AddComponent 다음에 정해지므로 폴더는 Start 에서 만든다
            Directory.CreateDirectory(Path.Combine(outputDir, "stills"));
            Directory.CreateDirectory(Path.Combine(outputDir, "frames"));
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Write($"start {DateTime.Now:yyyy-MM-dd HH:mm:ss}, scene {SceneManager.GetActiveScene().name}, {Screen.width}x{Screen.height}");

            // 1. 로비
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Still("01_lobby_play");

            var lobby = FindAnyObjectByType<LobbyScreen>();
            if (lobby != null)
            {
                lobby.ShowAgentTab();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Still("02_lobby_agents");
                Write($"deploy as {(GameSession.SelectedAgent != null ? GameSession.SelectedAgent.agentName : "?")}");
                lobby.Deploy();
            }
            else
            {
                Write("LobbyScreen not found, capturing current scene");
            }

            // 2. 미션 씬이 열릴 때까지
            float waitUntil = Time.realtimeSinceStartup + 10f;
            while (GameObject.FindWithTag("Player") == null && Time.realtimeSinceStartup < waitUntil) yield return null;
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
            {
                Write("ERROR: Player not found");
                Finish();
                yield break;
            }

            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<Health>();
            shooter = playerObject.GetComponent<PlayerShooter>();
            if (shooter != null && shooter.Gun != null) shooter.Gun.HitLanded += OnHit;
            foreach (var h in Health.All)
            {
                var victim = h;
                if (victim != playerHealth) victim.Died += () => OnKill(victim);
            }
            if (playerHealth != null) playerHealth.Died += () => Write($"player died at {Time.time:0.0}s");
            if (playerHealth != null) playerHealth.Damaged += OnPlayerDamaged;

            yield return new WaitForSecondsRealtime(1f);
            // 캐릭터가 벽 안에 생기지 않았는지 (NavMesh 0.5m 안에 있는지)
            foreach (var h in Health.All)
            {
                if (h == null) continue;
                bool onMesh = NavMesh.SamplePosition(h.transform.position, out _, 0.5f, NavMesh.AllAreas);
                Write($"spawn {h.DisplayName} ({h.transform.position.x:0.0}, {h.transform.position.z:0.0}) {(onMesh ? "on navmesh" : "NOT ON NAVMESH")}");
            }
            yield return Still("03_mission_start");

            // 3. 자동 조종하며 녹화 (게임 시간 기준 프레임 고정)
            input.MouseScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            GameInput.Simulated = input;
            Time.captureFramerate = FrameRate;
            recording = true;
            float startTime = Time.time;
            float nextStill = startTime + StillInterval;
            float deadTime = -1f;

            while (Time.time - startTime < RecordSeconds)
            {
                yield return new WaitForEndOfFrame();
                SaveFrame();

                if (pendingStill)
                {
                    pendingStill = false;
                    SaveStill(pendingStillName);
                }
                if (Time.time >= nextStill)
                {
                    nextStill += StillInterval;
                    SaveStill($"10_play_{Mathf.RoundToInt(Time.time - startTime):00}s");
                    LogCorpses();
                }
                if (playerHealth != null && playerHealth.IsDead)
                {
                    if (deadTime < 0f) deadTime = Time.time;
                    else if (Time.time - deadTime > 2f) break;
                }
            }

            recording = false;
            Finish();
        }

        private void Update()
        {
            if (!recording || player == null) return;
            Autopilot();
        }

        private void LateUpdate()
        {
            // "눌렀다" 입력은 한 프레임만 유지
            input.FirePressed = false;
            input.ReloadPressed = false;
            input.SpotPressed = false;
            input.MeleePressed = false;
        }

        private void Autopilot()
        {
            var cam = Camera.main;
            if (cam == null) return;

            target = NearestHostile();
            Vector3 self = player.position;
            var gun = shooter != null ? shooter.Gun : null;
            float range = gun != null && gun.Weapon != null ? gun.Weapon.range : 15f;

            bool visible = target != null && FieldOfView.Player != null && FieldOfView.Player.CanSee(target.transform.position);
            bool inRange = target != null && Flat.Distance(self, target.transform.position) < range * 0.75f;

            // 이동: 보이고 사거리 안이면 멈춰서 쏘고, 아니면 NavMesh 경로를 따라 다가간다
            Vector3 moveDir = Vector3.zero;
            if (target != null && !(visible && inRange))
            {
                if (Time.time >= nextPathTime)
                {
                    nextPathTime = Time.time + 0.5f;
                    waypoint = target.transform.position;
                    if (NavMesh.SamplePosition(self, out var from, 1.5f, NavMesh.AllAreas) &&
                        NavMesh.SamplePosition(target.transform.position, out var to, 1.5f, NavMesh.AllAreas) &&
                        NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) &&
                        path.corners.Length > 1)
                    {
                        waypoint = path.corners[1];
                    }
                }
                if (Flat.Distance(self, waypoint) < 0.4f) nextPathTime = 0f;
                moveDir = Flat.Direction(self, waypoint);
            }

            float yaw = CameraRig.Main != null ? CameraRig.Main.Yaw : 0f;
            Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * moveDir;
            input.Move = new Vector2(local.x, local.z);
            input.Run = false;

            // 사람처럼: 처음 본 순간부터 반응 시간을 재고, 조준점은 조금씩 흔들린다
            if (!visible) seenTarget = null;
            else if (target != seenTarget)
            {
                seenTarget = target;
                seenTime = Time.time;
            }
            if (Time.time >= nextAimOffsetTime)
            {
                nextAimOffsetTime = Time.time + 0.6f;
                Vector2 jitter = UnityEngine.Random.insideUnitCircle * AimError;
                aimOffset = new Vector3(jitter.x, 0f, jitter.y);
            }

            // 조준: 적이 보이면 적의 가슴(+흔들림), 아니면 가는 방향
            Vector3 lookAt = visible ? target.transform.position + aimOffset + Vector3.up * 1.1f
                                     : self + (moveDir.sqrMagnitude > 0.01f ? moveDir : player.forward) * 4f + Vector3.up * 1.1f;
            Vector3 screen = cam.WorldToScreenPoint(lookAt);
            input.MouseScreenPosition = new Vector2(screen.x, screen.y);
            input.Aim = visible && inRange;

            // 처음 본 적은 Q 로 스팟
            if (visible && target != lastSpotted)
            {
                lastSpotted = target;
                input.SpotPressed = true;
                Write($"spotted {target.DisplayName} at {Time.time:0.0}s");
                RequestStill("20_spotted_" + Safe(target.DisplayName));
            }

            // 사격: 조준이 잡힌 뒤
            bool shoot = visible && inRange && Time.time - seenTime >= ReactionTime &&
                         Vector3.Angle(player.forward, Flat.Direction(self, target.transform.position)) < 8f;
            if (gun != null && gun.Weapon != null && !gun.IsReloading &&
                (gun.Ammo <= 0 || (!visible && gun.Ammo <= gun.Weapon.magazineSize * 0.3f)))
                input.ReloadPressed = true;
            input.Fire = shoot;
            if (shoot && Time.time >= nextTriggerTime)
            {
                nextTriggerTime = Time.time + 0.25f;
                input.FirePressed = true;
            }
        }

        // 시체가 시야 밖에서도 보이는지 확인용: 시야 판정, 숨김 상태, 켜진 렌더러 수
        private void LogCorpses()
        {
            foreach (var h in Health.All)
            {
                if (h == null || !h.IsDead) continue;
                var hidden = h.GetComponent<HiddenWhenUnseen>();
                int on = 0, total = 0;
                foreach (var r in h.GetComponentsInChildren<Renderer>(true))
                {
                    total++;
                    if (r.enabled && r.gameObject.activeInHierarchy) on++;
                }
                bool canSee = FieldOfView.Player != null && FieldOfView.Player.CanSee(h.transform.position);
                Write($"corpse {h.DisplayName} d={Flat.Distance(player.position, h.transform.position):0.0} canSee={canSee} " +
                      $"hidden={(hidden != null ? (hidden.enabled ? (hidden.IsShown ? "shown" : "hiding") : "DISABLED") : "none")} renderersOn={on}/{total}");
            }
        }

        private Health NearestHostile()
        {
            Health best = null;
            float bestDistance = float.MaxValue;
            foreach (var h in Health.All)
            {
                if (h == null || h == playerHealth || h.IsDead || !Factions.AreHostile(Faction.Player, h.Faction)) continue;
                float d = Flat.Distance(player.position, h.transform.position);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = h;
                }
            }
            return best;
        }

        private void OnHit(Health victim, HitZone zone, float damage)
        {
            Write($"hit {victim.DisplayName} {zone} {damage:0} at {Time.time:0.0}s");
        }

        // 플레이어가 맞을 때: 누가, 얼마나, 쏜 쪽이 시야 안이었는지 (피격 방향 표시 확인용)
        private void OnPlayerDamaged(float amount, GameObject source)
        {
            if (source == null) return;
            bool seen = FieldOfView.Player != null && FieldOfView.Player.CanSee(source.transform.position);
            Write($"player hit {amount:0} by {source.name} d={Flat.Distance(player.position, source.transform.position):0.0} {(seen ? "in view" : "OFF VIEW")} hp={playerHealth.Current:0} at {Time.time:0.0}s");
            if (!seen) RequestStill("40_hit_offview");
        }

        private void OnKill(Health victim)
        {
            Write($"killed {victim.DisplayName} at {Time.time:0.0}s");
            RequestStill("30_kill_" + Safe(victim.DisplayName));
        }

        private void RequestStill(string name)
        {
            if (pendingStill) return;
            pendingStill = true;
            pendingStillName = name;
        }

        private IEnumerator Still(string name)
        {
            yield return new WaitForEndOfFrame();
            SaveStill(name);
        }

        private void SaveStill(string name)
        {
            string file = Path.Combine(outputDir, "stills", $"{stillIndex++:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(file);
            Write($"still {Path.GetFileName(file)}");
        }

        private void SaveFrame()
        {
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(outputDir, "frames", $"f_{frameIndex++:00000}.jpg"), texture.EncodeToJPG(85));
            Destroy(texture);
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Write($"[{type}] {message}\n{stackTrace}");
            else if (type == LogType.Warning)
                Write($"[Warning] {message}");
        }

        private void Write(string line)
        {
            log.AppendLine(line);
            Debug.Log("[AutoCapture] " + line);
        }

        private void Finish()
        {
            GameInput.Simulated = null;
            Time.captureFramerate = 0;
            Write($"done: {frameIndex} frames at {FrameRate} fps, {stillIndex} stills");
            File.WriteAllText(Path.Combine(outputDir, "log.txt"), log.ToString());
            Application.Quit();
        }

        private static string Safe(string s) => string.IsNullOrEmpty(s) ? "x" : s.Replace(' ', '_');
    }
}
