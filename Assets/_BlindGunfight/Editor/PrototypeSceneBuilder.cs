using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace BlindGunfight.EditorTools
{
    /// <summary>
    /// 메뉴 BlindGunfight > Build Prototype Scene 한 번으로 3D 쿼터뷰 테스트 씬을 만든다.
    /// 레이어, 재질, 무기 에셋, 총알 궤적 프리팹, 맵, 조명, 플레이어, 좀비, 무장 인간까지 만든다.
    /// 캐릭터는 임시 도형(캡슐)이다. 나중에 모델로 바꾼다.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/_BlindGunfight";
        private const string ArtFolder = Root + "/Art/Placeholder";
        private const string MaterialFolder = Root + "/Materials";
        private const string WeaponFolder = Root + "/Weapons";
        private const string PrefabFolder = Root + "/Prefabs";
        private const string SceneFolder = Root + "/Scenes";
        private const string ScenePath = SceneFolder + "/Prototype.unity";
        private const string LobbyScenePath = SceneFolder + "/Lobby.unity";
        private const string AgentFolder = Root + "/Agents";
        private const string ObstacleLayerName = "Obstacles";
        private const string CharacterLayerName = "Characters";

        private const float WallHeight = 2.5f;
        private const float CoverHeight = 1.2f;

        private static Sprite spotSprite;
        private static Sprite lastSeenSprite;
        private static Material unlitMaterial;
        private static int obstacleLayer;
        private static int characterLayer;
        private static LayerMask obstacleMask;

        [MenuItem("BlindGunfight/Build Prototype Scene")]
        public static void Build()
        {
            // 명령줄(-batchmode)로 실행할 때는 묻지 않고 바로 만든다
            if (!Application.isBatchMode && File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("BlindGunfight", "Prototype 씬을 새로 만들까요? 기존 Prototype 씬은 덮어씁니다.", "만들기", "취소"))
                return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            obstacleLayer = EnsureLayer(ObstacleLayerName);
            characterLayer = EnsureLayer(CharacterLayerName);
            if (obstacleLayer < 0 || characterLayer < 0)
            {
                EditorUtility.DisplayDialog("BlindGunfight", "User Layer 빈칸이 없어 레이어를 만들 수 없습니다.", "확인");
                return;
            }
            obstacleMask = 1 << obstacleLayer;

            EnsureFolder(MaterialFolder);
            EnsureFolder(WeaponFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);
            EnsureFolder(AgentFolder);

            // 새 씬(Single)을 열면 쓰이지 않는 에셋이 메모리에서 내려가 아래에서 만든 무기/요원 참조가 끊긴다. 씬을 먼저 연다.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureSprites();
            spotSprite = LoadSprite("marker_spot");
            lastSeenSprite = LoadSprite("marker_lastseen");
            unlitMaterial = LoadUnlitMaterial();

            var pistol = CreateWeapon("Pistol", "Pistol", damage: 30, pellets: 1, spread: 3, fireRate: 4, automatic: false, magazine: 12, reload: 1.4f, range: 20, noise: 20, recoil: 3f, maxRecoil: 8f);
            var smg = CreateWeapon("SMG", "SMG", damage: 20, pellets: 1, spread: 7, fireRate: 11, automatic: true, magazine: 30, reload: 1.8f, range: 15, noise: 20, recoil: 1.5f, maxRecoil: 9f);
            var shotgun = CreateWeapon("Shotgun", "Shotgun", damage: 15, pellets: 8, spread: 22, fireRate: 1.2f, automatic: false, magazine: 6, reload: 2.5f, range: 10, noise: 22, recoil: 6f, maxRecoil: 10f);
            var rifle = CreateWeapon("Rifle", "Rifle", damage: 25, pellets: 1, spread: 3, fireRate: 8, automatic: true, magazine: 30, reload: 2f, range: 25, noise: 22, recoil: 2f, maxRecoil: 10f);
            var smgSd = CreateWeapon("SMG-SD", "SMG-SD", damage: 18, pellets: 1, spread: 6, fireRate: 10, automatic: true, magazine: 30, reload: 1.8f, range: 14, noise: 7, recoil: 1.3f, maxRecoil: 8f);
            var dmr = CreateWeapon("DMR", "DMR", damage: 55, pellets: 1, spread: 1.5f, fireRate: 1.6f, automatic: false, magazine: 10, reload: 2.2f, range: 30, noise: 24, recoil: 5f, maxRecoil: 8f);
            var tracer = CreateTracerPrefab();

            // 구조팀 요원 4명 + 스토리로 열리는 잠긴 칸 2개
            var ethan = CreateAgent("Ethan_Cole", "Ethan Cole", "돌격", new Color(0.95f, 0.78f, 0.2f), 100f, 1f, rifle, pistol, "섬광탄", "파편수류탄",
                "국가 특수 구조팀 팀장. 헬기 추락에서 살아남아 흩어진 팀을 다시 모은다. 어떤 거리에서도 무난한 소총으로 팀의 중심을 잡는다.");
            var mira = CreateAgent("Mira_Vance", "Mira Vance", "정찰", new Color(0.3f, 0.75f, 0.95f), 90f, 1.1f, smgSd, pistol, "동작 감지기", "연막탄",
                "도시에 남은 민간인 해커. 소음기 기관단총으로 조용히 움직이며, 감지기로 벽 너머 움직임을 잡아낸다.");
            var marcus = CreateAgent("Marcus_Reyes", "Marcus Reyes", "돌파", new Color(0.85f, 0.35f, 0.25f), 120f, 0.92f, shotgun, pistol, "돌파 폭약", "파편수류탄",
                "구조팀 돌파 담당. 느리지만 단단하다. 산탄총으로 좁은 복도와 좀비 무리를 정리한다.");
            var lena = CreateAgent("Lena_Park", "Lena Park", "저격", new Color(0.55f, 0.85f, 0.45f), 90f, 1f, dmr, pistol, "표식 조준경", "섬광탄",
                "구조팀 지정사수. 한 발 한 발이 무겁다. 멀리서 머리를 노리면 Greywolf 도 한 번에 쓰러진다.");
            var locked1 = CreateAgent("Locked_1", "Unknown", "???", Color.gray, 100f, 1f, null, null, "", "", "스토리를 진행하면 합류합니다.", locked: true);
            var locked2 = CreateAgent("Locked_2", "Unknown", "???", Color.gray, 100f, 1f, null, null, "", "", "스토리를 진행하면 합류합니다.", locked: true);
            var roster = CreateRoster(ethan, mira, marcus, lena, locked1, locked2);
            AssetDatabase.SaveAssets();

            BuildLighting();
            BuildMap();
            var navMesh = new GameObject("NavMesh").AddComponent<RuntimeNavMesh>();
            SetVector3(navMesh, "size", new Vector3(44f, 10f, 34f));
            SetLayerMask(navMesh, "includeLayers", ~(1 << characterLayer));

            var player = BuildPlayer(new Vector3(-16f, 0f, -12f), pistol, tracer, ethan);
            BuildCamera(player.transform, new Vector2(-11f, -8.5f), new Vector2(11f, 6f));
            BuildHud(player);
            BuildZombies();
            BuildSoldiers(smg, rifle, shotgun, tracer);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            BuildLobbyScene(roster);
            AddSceneToBuildSettings(LobbyScenePath); // 로비가 0번(첫 화면)이 된다
            Debug.Log("[BlindGunfight] Lobby / Prototype 씬을 만들었습니다. 지금 열린 Lobby 씬에서 Play 를 누르세요.");
        }

        // ---------- 조명 ----------

        private static void BuildLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.11f, 0.14f);
            RenderSettings.fog = false;

            // 시야 밖도 지형은 어렴풋이 보이게 하는 약한 달빛
            var moon = new GameObject("Dim Directional Light").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.intensity = 0.25f;
            moon.color = new Color(0.6f, 0.68f, 0.85f);
            moon.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        }

        // ---------- 맵 ----------

        private static void BuildMap()
        {
            var map = new GameObject("Map").transform;
            var floorMat = CreateMaterial("Floor", new Color(0.32f, 0.33f, 0.35f));
            var wallMat = CreateMaterial("Wall", new Color(0.78f, 0.78f, 0.76f));
            var capMat = CreateMaterial("WallTop", new Color(0.05f, 0.05f, 0.06f));
            var crateMat = CreateMaterial("Crate", new Color(0.62f, 0.48f, 0.32f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(map);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(40f, 0.2f, 30f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // 바깥 벽 (40 x 30). 2D 좌표 (x, y) → 3D (x, z)
            Wall(map, wallMat, capMat, new Vector2(0f, 15f), new Vector2(41f, 1f));
            Wall(map, wallMat, capMat, new Vector2(0f, -15f), new Vector2(41f, 1f));
            Wall(map, wallMat, capMat, new Vector2(-20f, 0f), new Vector2(1f, 30f));
            Wall(map, wallMat, capMat, new Vector2(20f, 0f), new Vector2(1f, 30f));

            // 안쪽 벽 (통로 틈을 남긴다)
            Wall(map, wallMat, capMat, new Vector2(-8f, -5f), new Vector2(0.3f, 20f));
            Wall(map, wallMat, capMat, new Vector2(-8f, 12.5f), new Vector2(0.3f, 5f));
            Wall(map, wallMat, capMat, new Vector2(-5f, 2f), new Vector2(6f, 0.3f));
            Wall(map, wallMat, capMat, new Vector2(4f, 2f), new Vector2(6f, 0.3f));
            Wall(map, wallMat, capMat, new Vector2(7f, 5f), new Vector2(0.3f, 6f));
            Wall(map, wallMat, capMat, new Vector2(7f, 12.75f), new Vector2(0.3f, 4.5f));
            Wall(map, wallMat, capMat, new Vector2(10f, -9.5f), new Vector2(0.3f, 11f));

            // 엄폐물 (눈높이보다 낮아서 시야는 안 막고 총알은 막는다)
            Cover(map, crateMat, new Vector2(0f, -6f), new Vector2(2f, 2f));
            Cover(map, crateMat, new Vector2(-14f, -8f), new Vector2(1.5f, 1.5f));
            Cover(map, crateMat, new Vector2(14f, -5f), new Vector2(3f, 1f));
            Cover(map, crateMat, new Vector2(14f, 6f), new Vector2(1f, 3f));
            Cover(map, crateMat, new Vector2(-14f, 8f), new Vector2(2f, 1f));
            Cover(map, crateMat, new Vector2(3f, 9f), new Vector2(1.5f, 1.5f));
        }

        private static void Wall(Transform parent, Material wallMat, Material capMat, Vector2 center, Vector2 size)
        {
            var wall = Box(parent, "Wall", wallMat, new Vector3(center.x, WallHeight * 0.5f, center.y), new Vector3(size.x, WallHeight, size.y));
            wall.layer = obstacleLayer;

            // 블라인드스팟처럼 벽 윗면을 검게 칠해 단면처럼 보이게 한다.
            var cap = Box(wall.transform.parent, "Wall Top", capMat,
                          new Vector3(center.x, WallHeight + 0.01f, center.y), new Vector3(size.x + 0.02f, 0.04f, size.y + 0.02f));
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Cover(Transform parent, Material mat, Vector2 center, Vector2 size)
        {
            var box = Box(parent, "Crate", mat, new Vector3(center.x, CoverHeight * 0.5f, center.y), new Vector3(size.x, CoverHeight, size.y));
            box.layer = obstacleLayer;
        }

        private static GameObject Box(Transform parent, string name, Material mat, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // ---------- 플레이어 ----------

        private static void BuildLobbyScene(AgentRoster roster)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene 뒤에는 메모리의 roster 가 내려갔을 수 있으니 디스크에서 다시 읽는다
            roster = AssetDatabase.LoadAssetAtPath<AgentRoster>($"{AgentFolder}/AgentRoster.asset");

            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.gameObject.AddComponent<AudioListener>();

            var lobby = new GameObject("Lobby").AddComponent<LobbyScreen>();
            SetObject(lobby, "roster", roster);
            SetString(lobby, "missionScene", "Prototype");

            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        private static AgentData CreateAgent(string file, string agentName, string role, Color color, float hp, float speed,
                                             WeaponData primary, WeaponData secondary, string signatureTool, string tool,
                                             string description, bool locked = false)
        {
            string path = $"{AgentFolder}/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AgentData>(path);
            if (existing != null) return existing; // 직접 고친 요원 정보는 덮어쓰지 않는다

            var a = ScriptableObject.CreateInstance<AgentData>();
            a.agentName = agentName;
            a.role = role;
            a.themeColor = color;
            a.maxHealth = hp;
            a.moveSpeedMultiplier = speed;
            a.primaryWeapon = primary;
            a.secondaryWeapon = secondary;
            a.signatureTool = signatureTool;
            a.tool = tool;
            a.description = description;
            a.locked = locked;
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static AgentRoster CreateRoster(params AgentData[] agents)
        {
            string path = $"{AgentFolder}/AgentRoster.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AgentRoster>(path);
            if (existing != null) return existing;

            var roster = ScriptableObject.CreateInstance<AgentRoster>();
            roster.agents = agents;
            AssetDatabase.CreateAsset(roster, path);
            return roster;
        }

        private static GameObject BuildPlayer(Vector3 position, WeaponData weapon, BulletTracer tracer, AgentData defaultAgent)
        {
            var go = CreateCharacter("Player", "Ethan Cole", new Color(0.25f, 0.55f, 0.95f), position, Faction.Player, 100f);
            go.tag = "Player";

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 눈높이에서 앞을 비추는 시야 조명. 벽이 그림자를 만들어 안 보이는 곳이 어두워진다.
            var viewLight = new GameObject("View Light").AddComponent<Light>();
            viewLight.transform.SetParent(go.transform, false);
            viewLight.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            viewLight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            viewLight.type = LightType.Spot;
            viewLight.spotAngle = 90f;
            viewLight.innerSpotAngle = 72f;
            viewLight.range = 11f;
            viewLight.intensity = 6f;
            viewLight.color = new Color(1f, 0.97f, 0.9f);
            viewLight.shadows = LightShadows.Soft;

            var closeLight = new GameObject("Close Light").AddComponent<Light>();
            closeLight.transform.SetParent(go.transform, false);
            closeLight.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            closeLight.type = LightType.Point;
            closeLight.range = 3.5f;
            closeLight.intensity = 1.2f;
            closeLight.shadows = LightShadows.None;

            var fov = go.AddComponent<FieldOfView>();
            SetBool(fov, "isPlayerView", true);
            SetLayerMask(fov, "obstacleMask", obstacleMask);
            SetObject(fov, "viewLight", viewLight);

            var gun = AddGun(go, weapon, tracer);

            var controller = go.AddComponent<PlayerController>();
            SetObject(controller, "fieldOfView", fov);

            var shooter = go.AddComponent<PlayerShooter>();
            SetObject(shooter, "gun", gun);

            go.AddComponent<PlayerMelee>();
            go.AddComponent<PlayerSpotter>();

            var loadout = go.AddComponent<PlayerLoadout>();
            SetObject(loadout, "defaultAgent", defaultAgent);
            SetObject(loadout, "gun", gun);
            SetBool(go.GetComponent<DeathHandler>(), "restartSceneOnDeath", true);

            var laserGo = new GameObject("Laser Sight");
            laserGo.transform.SetParent(go.transform, false);
            var line = laserGo.AddComponent<LineRenderer>();
            line.widthMultiplier = 0.02f;
            line.sharedMaterial = unlitMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            var laser = laserGo.AddComponent<LaserSight>();
            SetObject(laser, "gun", gun);
            SetObject(laser, "controller", controller);

            return go;
        }

        private static void BuildCamera(Transform target, Vector2 boundsMin, Vector2 boundsMax)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = target.position + new Vector3(0f, 16f, -11f);
            go.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = 35f;
            cam.farClipPlane = 200f;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<CameraRig>();
            SetObject(rig, "target", target);
            SetVector2(rig, "boundsMin", boundsMin);
            SetVector2(rig, "boundsMax", boundsMax);
        }

        private static void BuildHud(GameObject player)
        {
            var hud = new GameObject("Debug HUD").AddComponent<DebugHud>();
            SetObject(hud, "playerHealth", player.GetComponent<Health>());
            SetObject(hud, "playerShooter", player.GetComponent<PlayerShooter>());
        }

        // ---------- 적 ----------

        private static void BuildZombies()
        {
            var parent = new GameObject("Zombies").transform;
            Vector2[] walkers = { new(-15f, 0f), new(-12f, 4f), new(-3f, -10f), new(2f, -12f), new(-4f, 10f), new(5f, -5f), new(17f, -12f), new(12f, 13f) };
            var walkerColor = new Color(0.45f, 0.65f, 0.35f);
            foreach (var pos in walkers)
                Zombie(parent, "Walker", walkerColor, pos, hp: 70, wander: 0.8f, chase: 2.5f, scream: false);

            var runnerColor = new Color(0.6f, 0.78f, 0.38f);
            Zombie(parent, "Runner", runnerColor, new Vector2(0f, 12f), hp: 50, wander: 1.2f, chase: 4f, scream: false);
            Zombie(parent, "Runner", runnerColor, new Vector2(-14f, 12f), hp: 50, wander: 1.2f, chase: 4f, scream: false);
            Zombie(parent, "Screamer", new Color(0.55f, 0.45f, 0.65f), new Vector2(-1f, -3f), hp: 90, wander: 0.7f, chase: 2.2f, scream: true);
        }

        private static void Zombie(Transform parent, string name, Color color, Vector2 pos, float hp, float wander, float chase, bool scream)
        {
            var go = CreateEnemy(parent, name, name, color, new Vector3(pos.x, 0f, pos.y), Faction.Zombie, hp);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var perception = go.AddComponent<Perception>();
            SetLayerMask(perception, "obstacleMask", obstacleMask);
            SetFloat(perception, "viewAngle", 120f);
            SetFloat(perception, "viewDistance", 6f);
            SetFloat(perception, "hearingMultiplier", 1.5f);

            var ai = go.AddComponent<ZombieAI>();
            SetFloat(ai, "wanderSpeed", wander);
            SetFloat(ai, "chaseSpeed", chase);
            SetFloat(ai, "attackRange", 1.1f);
            SetBool(ai, "screamsOnSight", scream);
        }

        private static void BuildSoldiers(WeaponData smg, WeaponData rifle, WeaponData shotgun, BulletTracer tracer)
        {
            var parent = new GameObject("Greywolf").transform;
            var waypointRoot = new GameObject("Waypoints").transform;

            // 한곳에 뭉치지 않도록 남쪽 / 동쪽 / 북서쪽에 둘씩 나눠 둔다
            Soldier(parent, waypointRoot, "Greywolf Rifleman", new Vector2(3f, -12f), new[] { new Vector2(3f, -12f), new Vector2(5f, -3f) }, rifle, tracer);
            Soldier(parent, waypointRoot, "Greywolf Breacher", new Vector2(-2f, -9f), new Vector2[0], shotgun, tracer);
            Soldier(parent, waypointRoot, "Greywolf Gunner", new Vector2(16f, 10f), new[] { new Vector2(16f, 10f), new Vector2(11f, 12f) }, smg, tracer);
            Soldier(parent, waypointRoot, "Greywolf Marksman", new Vector2(17f, 6f), new Vector2[0], rifle, tracer);
            Soldier(parent, waypointRoot, "Greywolf Pointman", new Vector2(-3f, 12f), new[] { new Vector2(-3f, 12f), new Vector2(4f, 12f) }, smg, tracer);
            Soldier(parent, waypointRoot, "Greywolf Scout", new Vector2(-14f, 11f), new[] { new Vector2(-14f, 11f), new Vector2(-11f, 4f) }, rifle, tracer);
        }

        private static void Soldier(Transform parent, Transform waypointRoot, string name, Vector2 pos, Vector2[] route,
                                    WeaponData weapon, BulletTracer tracer)
        {
            var go = CreateEnemy(parent, name, name, new Color(0.8f, 0.25f, 0.22f), new Vector3(pos.x, 0f, pos.y), Faction.Mercenary, 140f);
            go.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            var perception = go.AddComponent<Perception>();
            SetLayerMask(perception, "obstacleMask", obstacleMask);
            SetFloat(perception, "viewDistance", 8f); // 플레이어(7m)보다 멀리 보지 않게

            var gun = AddGun(go, weapon, tracer);

            var waypoints = new Object[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                var wp = new GameObject($"{name} WP{i}").transform;
                wp.SetParent(waypointRoot);
                wp.position = new Vector3(route[i].x, 0f, route[i].y);
                waypoints[i] = wp;
            }

            var ai = go.AddComponent<SoldierAI>();
            SetObject(ai, "gun", gun);
            SetFloat(ai, "reactionTime", 0.35f);
            SetFloat(ai, "inaccuracy", 6f);
            SetObjectArray(ai, "waypoints", waypoints);
        }

        private static GameObject CreateEnemy(Transform parent, string name, string displayName, Color color, Vector3 position, Faction faction, float hp)
        {
            var go = CreateCharacter(name, displayName, color, position, faction, hp);
            go.transform.SetParent(parent);

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.enabled = false; // EnemyMotor.Start 에서 NavMesh 가 생긴 뒤 켠다

            go.AddComponent<EnemyMotor>();
            go.AddComponent<HiddenWhenUnseen>();

            var spottable = go.AddComponent<Spottable>();
            SetObject(spottable, "spottedSprite", spotSprite);
            SetObject(spottable, "lastSeenSprite", lastSeenSprite);
            SetObject(spottable, "markerMaterial", unlitMaterial);
            return go;
        }

        // ---------- 공통 ----------

        /// <summary>임시 캐릭터: 몸통 캡슐 + 머리 + 앞을 가리키는 총. 앞 = +Z</summary>
        private static GameObject CreateCharacter(string name, string displayName, Color color, Vector3 position, Faction faction, float hp)
        {
            var go = new GameObject(name) { layer = characterLayer };
            go.transform.position = position;

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.8f;
            col.radius = 0.35f;

            var bodyMat = CreateMaterial("Char_" + ColorUtility.ToHtmlStringRGB(color), color);
            var darkMat = CreateMaterial("Char_Dark", new Color(0.12f, 0.12f, 0.13f));
            Part(go.transform, PrimitiveType.Capsule, bodyMat, new Vector3(0f, 0.85f, 0f), new Vector3(0.65f, 0.75f, 0.5f));
            Part(go.transform, PrimitiveType.Sphere, bodyMat, new Vector3(0f, 1.65f, 0.02f), new Vector3(0.32f, 0.32f, 0.32f));
            Part(go.transform, PrimitiveType.Cube, darkMat, new Vector3(0.18f, 1.1f, 0.45f), new Vector3(0.08f, 0.1f, 0.6f));

            var health = go.AddComponent<Health>();
            SetEnum(health, "faction", (int)faction);
            SetFloat(health, "maxHealth", hp);
            SetString(health, "displayName", displayName);

            go.AddComponent<DeathHandler>();
            return go;
        }

        private static void Part(Transform parent, PrimitiveType type, Material mat, Vector3 localPos, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Gun AddGun(GameObject owner, WeaponData weapon, BulletTracer tracer)
        {
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(owner.transform, false);
            muzzle.localPosition = new Vector3(0.18f, 1.1f, 0.8f);

            var gun = owner.AddComponent<Gun>();
            SetObject(gun, "weapon", weapon);
            SetObject(gun, "muzzle", muzzle);
            SetObject(gun, "tracerPrefab", tracer);
            return gun;
        }

        private static WeaponData CreateWeapon(string file, string displayName, float damage, int pellets, float spread,
                                               float fireRate, bool automatic, int magazine, float reload, float range, float noise,
                                               float recoil, float maxRecoil)
        {
            string path = $"{WeaponFolder}/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (existing != null) return existing; // 직접 조정한 값은 덮어쓰지 않는다

            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.displayName = displayName;
            w.damage = damage;
            w.pellets = pellets;
            w.spread = spread;
            w.fireRate = fireRate;
            w.automatic = automatic;
            w.magazineSize = magazine;
            w.reloadTime = reload;
            w.range = range;
            w.noiseRadius = noise;
            w.recoilPerShot = recoil;
            w.maxRecoil = maxRecoil;
            AssetDatabase.CreateAsset(w, path);
            return w;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static BulletTracer CreateTracerPrefab()
        {
            string path = PrefabFolder + "/BulletTracer.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<BulletTracer>();

            var go = new GameObject("BulletTracer");
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.05f;
            line.startColor = new Color(1f, 0.9f, 0.5f, 0.9f);
            line.endColor = new Color(1f, 0.9f, 0.5f, 0.2f);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.sharedMaterial = LoadUnlitMaterial();
            go.AddComponent<BulletTracer>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<BulletTracer>();
        }

        private static Material LoadUnlitMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat")
                   ?? AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        private static void ConfigureSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
            if (sprite == null) Debug.LogWarning($"[BlindGunfight] 스프라이트를 찾지 못했습니다: {ArtFolder}/{name}.png");
            return sprite;
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(element.stringValue)) continue;
                element.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            return -1;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- private [SerializeField] 값 넣기 ----------

        private static void Set(Object target, string property, System.Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogWarning($"[BlindGunfight] {target.GetType().Name}.{property} 를 찾지 못했습니다.");
                return;
            }
            apply(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object t, string p, float v) => Set(t, p, x => x.floatValue = v);
        private static void SetBool(Object t, string p, bool v) => Set(t, p, x => x.boolValue = v);
        private static void SetString(Object t, string p, string v) => Set(t, p, x => x.stringValue = v);
        private static void SetEnum(Object t, string p, int v) => Set(t, p, x => x.enumValueIndex = v);
        private static void SetVector3(Object t, string p, Vector3 v) => Set(t, p, x => x.vector3Value = v);
        private static void SetVector2(Object t, string p, Vector2 v) => Set(t, p, x => x.vector2Value = v);
        private static void SetLayerMask(Object t, string p, LayerMask v) => Set(t, p, x => x.intValue = v.value);
        private static void SetObject(Object t, string p, Object v) => Set(t, p, x => x.objectReferenceValue = v);

        private static void SetObjectArray(Object t, string p, Object[] values) => Set(t, p, x =>
        {
            x.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) x.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        });
    }
}
