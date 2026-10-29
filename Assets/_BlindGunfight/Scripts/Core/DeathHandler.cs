using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace BlindGunfight
{
    /// <summary>
    /// 죽으면 움직임과 AI를 끄고 바닥에 쓰러진 시체로 남긴다. 플레이어는 잠시 뒤 씬을 다시 시작한다.
    /// (나중에 캐릭터 모델이 들어오면 쓰러지는 애니메이션이나 래그돌로 바꾼다.)
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DeathHandler : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color corpseTint = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private bool restartSceneOnDeath = false;
        [SerializeField, Min(0f)] private float restartDelay = 3f;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDied()
        {
            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == this || behaviour is Health || behaviour is HiddenWhenUnseen) continue;
                behaviour.enabled = false;
            }

            if (TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;
            if (TryGetComponent(out Rigidbody rb))
            {
                if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

            // 옆으로 쓰러뜨린다.
            transform.SetPositionAndRotation(
                new Vector3(transform.position.x, 0.3f, transform.position.z),
                Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Quaternion.Euler(0f, 0f, 90f));

            var block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is SpriteRenderer || r is LineRenderer) continue;
                r.GetPropertyBlock(block);
                Color baseColor = r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId)
                    ? r.sharedMaterial.GetColor(BaseColorId)
                    : Color.white;
                block.SetColor(BaseColorId, baseColor * corpseTint);
                r.SetPropertyBlock(block);
            }

            foreach (var light in GetComponentsInChildren<Light>()) light.enabled = false;

            if (restartSceneOnDeath) Invoke(nameof(RestartScene), restartDelay);
        }

        private void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
