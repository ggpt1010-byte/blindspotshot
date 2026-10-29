using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlindGunfight
{
    /// <summary>
    /// 블라인드스팟 로비를 따라 만든 메인 화면.
    /// 위쪽 탭(플레이 / 요원 / 기록, Q·E 로 이동), 플레이 탭의 로고와 SELECT MODE 카드,
    /// 요원 탭의 요원 목록 / 큰 그림 / 정보 패널 / 출격 버튼. 화면은 실행할 때 코드로 만든다.
    /// </summary>
    public class LobbyScreen : MonoBehaviour
    {
        private enum Tab { Play, Agents, Records }

        [SerializeField] private AgentRoster roster;
        [SerializeField] private string missionScene = GameSession.MissionScene;

        private static readonly Color Background = new Color(0.07f, 0.05f, 0.05f, 1f);
        private static readonly Color Accent = new Color(0.93f, 0.2f, 0.27f, 1f);
        private static readonly Color Selected = new Color(0.35f, 0.95f, 0.6f, 1f);
        private static readonly Color Muted = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color CardColor = new Color(0.13f, 0.12f, 0.13f, 1f);

        private readonly Dictionary<Tab, RectTransform> pages = new Dictionary<Tab, RectTransform>();
        private readonly Dictionary<Tab, Text> tabLabels = new Dictionary<Tab, Text>();
        private readonly Dictionary<Tab, Image> tabUnderlines = new Dictionary<Tab, Image>();
        private readonly List<(AgentData agent, Outline frame)> tiles = new List<(AgentData, Outline)>();

        private Tab currentTab;
        private AgentData selectedAgent;

        private Image portraitImage;
        private Text portraitInitials;
        private Text nameText;
        private Text roleText;
        private Text descriptionText;
        private Text primaryText;
        private Text secondaryText;
        private Text signatureToolText;
        private Text toolText;
        private Text statsText;

        private void Awake()
        {
            UIFactory.EnsureEventSystem();
            selectedAgent = GameSession.SelectedAgent != null ? GameSession.SelectedAgent : roster != null ? roster.FirstUnlocked() : null;

            var canvas = UIFactory.CreateCanvas("Lobby Canvas");
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Panel(root, "Background", Background, Vector2.zero, Vector2.one);
            BuildTopBar(root);
            pages[Tab.Play] = BuildPlayPage(root);
            pages[Tab.Agents] = BuildAgentPage(root);
            pages[Tab.Records] = BuildRecordsPage(root);

            ShowTab(Tab.Play);
            RefreshAgent();
        }

        private void Update()
        {
            if (GameInput.TabLeftPressed) ShowTab((Tab)Mathf.Max(0, (int)currentTab - 1));
            if (GameInput.TabRightPressed) ShowTab((Tab)Mathf.Min(2, (int)currentTab + 1));
            if (GameInput.BackPressed && currentTab != Tab.Play) ShowTab(Tab.Play);
        }

        // ---------- 위쪽 탭 ----------

        private void BuildTopBar(Transform root)
        {
            var bar = UIFactory.Rect(root, "Top Bar", new Vector2(0f, 0.93f), new Vector2(1f, 1f));
            UIFactory.Panel(bar, "Line", new Color(1f, 1f, 1f, 0.15f), new Vector2(0.02f, 0f), new Vector2(0.98f, 0.02f));

            UIFactory.Label(bar, "Q", 16, Muted, TextAnchor.MiddleCenter, new Vector2(0.26f, 0f), new Vector2(0.29f, 1f), FontStyle.Bold);
            UIFactory.Label(bar, "E", 16, Muted, TextAnchor.MiddleCenter, new Vector2(0.71f, 0f), new Vector2(0.74f, 1f), FontStyle.Bold);

            AddTab(bar, Tab.Play, "플레이", 0.30f);
            AddTab(bar, Tab.Agents, "요원", 0.44f);
            AddTab(bar, Tab.Records, "기록", 0.58f);
        }

        private void AddTab(Transform bar, Tab tab, string label, float x)
        {
            var button = UIFactory.CreateButton(bar, "Tab " + tab, new Color(0f, 0f, 0f, 0f), new Vector2(x, 0f), new Vector2(x + 0.12f, 1f), () => ShowTab(tab));
            tabLabels[tab] = UIFactory.Label(button.transform, label, 22, Muted, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            tabUnderlines[tab] = UIFactory.Panel(button.transform, "Underline", Color.white, new Vector2(0.2f, 0.05f), new Vector2(0.8f, 0.09f));
        }

        private void ShowTab(Tab tab)
        {
            currentTab = tab;
            foreach (var pair in pages) pair.Value.gameObject.SetActive(pair.Key == tab);
            foreach (var pair in tabLabels) pair.Value.color = pair.Key == tab ? Color.white : Muted;
            foreach (var pair in tabUnderlines) pair.Value.enabled = pair.Key == tab;
        }

        // ---------- 플레이 탭 ----------

        private RectTransform BuildPlayPage(Transform root)
        {
            var page = UIFactory.Rect(root, "Play Page", Vector2.zero, new Vector2(1f, 0.93f));

            // 배경 분위기: 붉은 빛
            UIFactory.Panel(page, "Glow", new Color(0.55f, 0.12f, 0.08f, 0.35f), new Vector2(0.35f, 0.25f), new Vector2(1f, 1f));

            UIFactory.Label(page, "PORT HALDEN", 20, Accent, TextAnchor.LowerLeft, new Vector2(0.03f, 0.83f), new Vector2(0.4f, 0.88f), FontStyle.Bold);
            UIFactory.Label(page, "BLIND\nGUNFIGHT", 64, Accent, TextAnchor.UpperLeft, new Vector2(0.03f, 0.62f), new Vector2(0.4f, 0.83f), FontStyle.Bold);
            UIFactory.Label(page, "봉쇄된 항구 도시에서 살아남은 구조팀.\n보이는 것만 믿을 수 있다.", 22, Color.white, TextAnchor.UpperLeft,
                            new Vector2(0.03f, 0.5f), new Vector2(0.4f, 0.6f), FontStyle.Bold);
            UIFactory.Label(page, "요원을 고르고 출격하세요. 요원마다 주무기, 보조무기, 도구가 다릅니다.", 16, Muted, TextAnchor.UpperLeft,
                            new Vector2(0.03f, 0.44f), new Vector2(0.4f, 0.5f));

            UIFactory.Label(page, "SELECT MODE", 26, Color.white, TextAnchor.LowerLeft, new Vector2(0.02f, 0.31f), new Vector2(0.3f, 0.36f), FontStyle.Bold);

            // 안내 말풍선 (블라인드스팟의 빨간 툴팁)
            var tip = UIFactory.Panel(page, "Tip", Accent, new Vector2(0.03f, 0.365f), new Vector2(0.2f, 0.4f));
            UIFactory.Label(tip.transform, "캠페인으로 시작해 보세요", 15, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            AddModeCard(page, 0, "캠페인", "Port Halden 탈출 작전", new Color(0.45f, 0.22f, 0.12f), false, OpenAgentSelect);
            AddModeCard(page, 1, "미션 선택", "캠페인 진행 후 열림", new Color(0.2f, 0.2f, 0.22f), true, null);
            AddModeCard(page, 2, "사격장", "무기와 반동 연습", new Color(0.05f, 0.35f, 0.3f), false, OpenAgentSelect);
            AddModeCard(page, 3, "튜토리얼", "준비 중", new Color(0.05f, 0.3f, 0.25f), true, null);
            return page;
        }

        private void AddModeCard(Transform page, int index, string title, string subtitle, Color color, bool locked, System.Action onClick)
        {
            float x0 = 0.02f + index * 0.155f;
            var card = UIFactory.CreateButton(page, "Mode " + title, locked ? new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f) : color,
                                        new Vector2(x0, 0.05f), new Vector2(x0 + 0.145f, 0.3f), onClick);
            card.interactable = !locked;

            UIFactory.Panel(card.transform, "Shade", new Color(0f, 0f, 0f, 0.55f), Vector2.zero, new Vector2(1f, 0.32f));
            UIFactory.Label(card.transform, title, 22, Color.white, TextAnchor.MiddleLeft, new Vector2(0.07f, 0.14f), new Vector2(1f, 0.3f), FontStyle.Bold);
            UIFactory.Label(card.transform, subtitle, 14, Muted, TextAnchor.MiddleLeft, new Vector2(0.07f, 0.02f), new Vector2(1f, 0.15f));
            if (locked)
                UIFactory.Label(card.transform, "LOCKED", 18, Muted, TextAnchor.MiddleCenter, new Vector2(0f, 0.45f), new Vector2(1f, 0.8f), FontStyle.Bold);
        }

        private void OpenAgentSelect() => ShowTab(Tab.Agents);

        // ---------- 요원 탭 ----------

        private RectTransform BuildAgentPage(Transform root)
        {
            var page = UIFactory.Rect(root, "Agent Page", Vector2.zero, new Vector2(1f, 0.93f));
            UIFactory.Panel(page, "Glow", new Color(0.15f, 0.2f, 0.3f, 0.35f), new Vector2(0.25f, 0f), new Vector2(0.75f, 1f));

            // 왼쪽: 요원 목록
            UIFactory.Label(page, "요원 목록", 20, Color.white, TextAnchor.MiddleLeft, new Vector2(0.02f, 0.9f), new Vector2(0.25f, 0.95f), FontStyle.Bold);
            UIFactory.Panel(page, "Line", new Color(1f, 1f, 1f, 0.2f), new Vector2(0.02f, 0.895f), new Vector2(0.26f, 0.898f));
            var grid = UIFactory.Rect(page, "Agent Grid", new Vector2(0.02f, 0.35f), new Vector2(0.26f, 0.885f));
            if (roster != null)
            {
                for (int i = 0; i < roster.agents.Length; i++) AddAgentTile(grid, i, roster.agents[i]);
            }

            // 가운데: 큰 요원 그림 (그림이 없으면 머리글자)
            portraitImage = UIFactory.Panel(page, "Portrait", Color.gray, new Vector2(0.33f, 0.04f), new Vector2(0.67f, 0.92f));
            portraitImage.preserveAspect = true;
            portraitInitials = UIFactory.Label(portraitImage.transform, "", 160, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter,
                                               Vector2.zero, Vector2.one, FontStyle.Bold);

            // 오른쪽: 정보 패널
            var info = UIFactory.Rect(page, "Info", new Vector2(0.72f, 0.2f), new Vector2(0.97f, 0.9f));
            nameText = UIFactory.Label(info, "", 44, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 0.88f), new Vector2(1f, 1f), FontStyle.Bold);
            roleText = UIFactory.Label(info, "", 18, Muted, TextAnchor.MiddleLeft, new Vector2(0f, 0.82f), new Vector2(1f, 0.88f));
            descriptionText = UIFactory.Label(info, "", 17, new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperLeft, new Vector2(0f, 0.6f), new Vector2(1f, 0.81f));
            statsText = UIFactory.Label(info, "", 16, Muted, TextAnchor.MiddleLeft, new Vector2(0f, 0.53f), new Vector2(1f, 0.59f));

            UIFactory.Panel(info, "Line", new Color(1f, 1f, 1f, 0.2f), new Vector2(0f, 0.515f), new Vector2(1f, 0.518f));
            UIFactory.Label(info, "주 무기", 15, Muted, TextAnchor.MiddleLeft, new Vector2(0f, 0.45f), new Vector2(0.5f, 0.5f));
            UIFactory.Label(info, "보조 무기", 15, Muted, TextAnchor.MiddleLeft, new Vector2(0.52f, 0.45f), new Vector2(1f, 0.5f));
            primaryText = UIFactory.Label(info, "", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 0.36f), new Vector2(0.5f, 0.45f), FontStyle.Bold);
            secondaryText = UIFactory.Label(info, "", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0.52f, 0.36f), new Vector2(1f, 0.45f), FontStyle.Bold);

            UIFactory.Panel(info, "Line", new Color(1f, 1f, 1f, 0.2f), new Vector2(0f, 0.335f), new Vector2(1f, 0.338f));
            UIFactory.Label(info, "전용 도구", 15, Muted, TextAnchor.MiddleLeft, new Vector2(0f, 0.27f), new Vector2(0.5f, 0.32f));
            UIFactory.Label(info, "도구", 15, Muted, TextAnchor.MiddleLeft, new Vector2(0.52f, 0.27f), new Vector2(1f, 0.32f));
            signatureToolText = UIFactory.Label(info, "", 22, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 0.18f), new Vector2(0.5f, 0.27f), FontStyle.Bold);
            toolText = UIFactory.Label(info, "", 22, Color.white, TextAnchor.MiddleLeft, new Vector2(0.52f, 0.18f), new Vector2(1f, 0.27f), FontStyle.Bold);
            UIFactory.Label(info, "1 / 2 키로 주 무기와 보조 무기를 바꿉니다", 14, Muted, TextAnchor.MiddleLeft, new Vector2(0f, 0.1f), new Vector2(1f, 0.16f));

            // 출격 버튼
            var deploy = UIFactory.CreateButton(page, "Deploy", Accent, new Vector2(0.8f, 0.05f), new Vector2(0.97f, 0.14f), Deploy);
            UIFactory.Label(deploy.transform, "출격", 30, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, FontStyle.Bold);

            return page;
        }

        private void AddAgentTile(Transform grid, int index, AgentData agent)
        {
            if (agent == null) return;

            const int columns = 4;
            int col = index % columns;
            int row = index / columns;
            float w = 1f / columns;
            float h = 0.24f;
            var min = new Vector2(col * w + 0.01f, 1f - (row + 1) * h + 0.01f);
            var max = new Vector2((col + 1) * w - 0.01f, 1f - row * h - 0.01f);

            Color tileColor = agent.locked ? new Color(0.18f, 0.18f, 0.2f) : agent.themeColor * 0.75f;
            tileColor.a = 1f;
            var tile = UIFactory.CreateButton(grid, "Agent " + agent.agentName, tileColor, min, max, () => SelectAgent(agent));
            tile.interactable = !agent.locked;

            if (agent.portrait != null && !agent.locked)
            {
                var img = tile.GetComponent<Image>();
                img.sprite = agent.portrait;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                UIFactory.Label(tile.transform, agent.locked ? "LOCK" : agent.Initials, agent.locked ? 16 : 30,
                                agent.locked ? Muted : Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, FontStyle.Bold);
            }

            var outline = tile.gameObject.AddComponent<Outline>();
            outline.effectColor = Selected;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.enabled = false;
            tiles.Add((agent, outline));
        }

        private void SelectAgent(AgentData agent)
        {
            if (agent == null || agent.locked) return;
            selectedAgent = agent;
            GameSession.SelectedAgent = agent;
            RefreshAgent();
        }

        private void RefreshAgent()
        {
            foreach (var (agent, frame) in tiles) frame.enabled = agent == selectedAgent;
            if (selectedAgent == null || nameText == null) return;

            var a = selectedAgent;
            nameText.text = $"[ {a.agentName} ]";
            roleText.text = $"✕  |  {a.role}";
            descriptionText.text = a.description;
            statsText.text = $"체력 {Mathf.RoundToInt(a.maxHealth)}   이동 속도 ×{a.moveSpeedMultiplier:0.00}";
            primaryText.text = a.primaryWeapon != null ? a.primaryWeapon.displayName : "-";
            secondaryText.text = a.secondaryWeapon != null ? a.secondaryWeapon.displayName : "-";
            signatureToolText.text = string.IsNullOrEmpty(a.signatureTool) ? "-" : a.signatureTool;
            toolText.text = string.IsNullOrEmpty(a.tool) ? "-" : a.tool;

            if (a.portrait != null)
            {
                portraitImage.sprite = a.portrait;
                portraitImage.color = Color.white;
                portraitInitials.text = "";
            }
            else
            {
                portraitImage.sprite = null;
                Color c = a.themeColor * 0.45f;
                c.a = 1f;
                portraitImage.color = c;
                portraitInitials.text = a.Initials;
            }
        }

        /// <summary>자동 촬영(AutoCapture)에서 요원 탭을 열 때 쓴다.</summary>
        public void ShowAgentTab() => ShowTab(Tab.Agents);

        public void Deploy()
        {
            if (selectedAgent == null) return;
            GameSession.StartMission(selectedAgent, missionScene);
        }

        // ---------- 기록 탭 ----------

        private RectTransform BuildRecordsPage(Transform root)
        {
            var page = UIFactory.Rect(root, "Records Page", Vector2.zero, new Vector2(1f, 0.93f));
            UIFactory.Label(page, "기록", 40, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 0.55f), new Vector2(1f, 0.65f), FontStyle.Bold);
            UIFactory.Label(page, "미션 결과, 처치 수, 명중률(부위별)이 여기에 쌓일 예정이에요.", 20, Muted, TextAnchor.MiddleCenter,
                            new Vector2(0f, 0.45f), new Vector2(1f, 0.55f));
            return page;
        }
    }
}
