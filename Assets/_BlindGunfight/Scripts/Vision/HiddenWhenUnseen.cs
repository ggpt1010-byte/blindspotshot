using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 플레이어 시야 밖에 있으면 이 오브젝트(적, 시체 등)의 그림을 숨긴다.
    /// </summary>
    public class HiddenWhenUnseen : MonoBehaviour
    {
        [Tooltip("시야에서 벗어난 뒤 이 시간(초) 동안은 계속 보여준다. 깜빡임 방지.")]
        [SerializeField, Min(0f)] private float revealGrace = 0.1f;

        private Renderer[] renderers;
        private float lastSeenTime = float.NegativeInfinity;

        public bool IsShown { get; private set; } = true;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            SetShown(false);
        }

        private void LateUpdate()
        {
            var view = FieldOfView.Player;
            if (view == null || view.CanSee(transform.position)) lastSeenTime = Time.time;

            SetShown(Time.time - lastSeenTime <= revealGrace);
        }

        private void SetShown(bool value)
        {
            if (IsShown == value) return;
            IsShown = value;
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = value;
            }
        }
    }
}
