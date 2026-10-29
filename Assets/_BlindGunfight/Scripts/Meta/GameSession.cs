using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlindGunfight
{
    /// <summary>
    /// 씬이 바뀌어도 남는 선택 정보 (로비에서 고른 요원 등).
    /// </summary>
    public static class GameSession
    {
        public const string LobbyScene = "Lobby";
        public const string MissionScene = "Prototype";

        public static AgentData SelectedAgent { get; set; }

        public static void StartMission(AgentData agent, string sceneName = MissionScene)
        {
            SelectedAgent = agent;
            SceneManager.LoadScene(sceneName);
        }

        public static void ReturnToLobby()
        {
            if (Application.CanStreamedLevelBeLoaded(LobbyScene)) SceneManager.LoadScene(LobbyScene);
            else Debug.LogWarning("[BlindGunfight] Lobby 씬이 Build Settings 에 없습니다.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SelectedAgent = null;
        }
    }
}
