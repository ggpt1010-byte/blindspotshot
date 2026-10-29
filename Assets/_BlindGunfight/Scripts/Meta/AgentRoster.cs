using UnityEngine;

namespace BlindGunfight
{
    /// <summary>요원 선택 화면에 보일 요원 목록 (순서대로 표시)</summary>
    [CreateAssetMenu(menuName = "BlindGunfight/Agent Roster", fileName = "AgentRoster")]
    public class AgentRoster : ScriptableObject
    {
        public AgentData[] agents = new AgentData[0];

        public AgentData FirstUnlocked()
        {
            foreach (var agent in agents)
            {
                if (agent != null && !agent.locked) return agent;
            }
            return null;
        }
    }
}
