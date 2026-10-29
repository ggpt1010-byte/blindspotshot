using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// Q 키 스포팅. 보이는 적 중 마우스 커서에 가장 가까운 적을 표시한다.
    /// 커서 근처에 없으면 시야 안에서 플레이어와 가장 가까운 적을 표시한다.
    /// </summary>
    public class PlayerSpotter : MonoBehaviour
    {
        [Tooltip("커서에서 이 거리(m) 안의 적을 우선 표시")]
        [SerializeField] private float cursorRadius = 3f;
        [SerializeField] private float cooldown = 0.25f;

        private float nextSpotTime;

        private void Update()
        {
            if (!GameInput.SpotPressed || Time.time < nextSpotTime) return;
            nextSpotTime = Time.time + cooldown;

            var view = FieldOfView.Player;
            if (view == null) return;

            Vector3 cursor = CameraRig.Main != null ? CameraRig.Main.AimPoint : transform.position;

            Spottable nearCursor = null, nearPlayer = null;
            float bestCursor = cursorRadius, bestPlayer = float.MaxValue;

            foreach (var target in Spottable.All)
            {
                Vector3 pos = target.transform.position;
                if (!view.CanSee(pos)) continue;

                float dc = Flat.Distance(pos, cursor);
                if (dc < bestCursor)
                {
                    bestCursor = dc;
                    nearCursor = target;
                }

                float dp = Flat.Distance(pos, transform.position);
                if (dp < bestPlayer)
                {
                    bestPlayer = dp;
                    nearPlayer = target;
                }
            }

            var chosen = nearCursor != null ? nearCursor : nearPlayer;
            if (chosen != null) chosen.Spot();
        }
    }
}
