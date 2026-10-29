using UnityEngine;

namespace BlindGunfight
{
    /// <summary>총알 궤적을 아주 잠깐 그리고 사라진다.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class BulletTracer : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float lifetime = 0.06f;

        private LineRenderer line;
        private float age;
        private Color startColor;
        private Color endColor;

        public void Show(Vector3 from, Vector3 to)
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            startColor = line.startColor;
            endColor = line.endColor;
            age = 0f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float alpha = 1f - age / lifetime;
            if (alpha <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            line.startColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a * alpha);
            line.endColor = new Color(endColor.r, endColor.g, endColor.b, endColor.a * alpha);
        }
    }
}
