using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BlindGunfight
{
    /// <summary>
    /// 게임이 시작될 때 맵의 콜라이더로 적 길찾기용 NavMesh 를 만든다.
    /// AI Navigation 패키지 없이 Unity 기본 기능만 쓴다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class RuntimeNavMesh : MonoBehaviour
    {
        [Tooltip("NavMesh 를 만들 영역 크기 (이 오브젝트 위치가 중심)")]
        [SerializeField] private Vector3 size = new Vector3(60f, 10f, 50f);
        [Tooltip("길찾기에 포함할 레이어 (바닥, 벽). 캐릭터 레이어는 빼야 한다")]
        [SerializeField] private LayerMask includeLayers = ~0;
        [SerializeField] private float agentRadius = 0.35f;

        private NavMeshDataInstance instance;

        private void Awake()
        {
            var bounds = new Bounds(transform.position, size);
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, includeLayers, NavMeshCollectGeometry.PhysicsColliders, 0,
                                          new List<NavMeshBuildMarkup>(), sources);

            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = agentRadius;

            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
            {
                Debug.LogWarning("[BlindGunfight] NavMesh 를 만들지 못했습니다. 바닥 콜라이더가 있는지 확인하세요.");
                return;
            }
            instance = NavMesh.AddNavMeshData(data);
        }

        private void OnDestroy()
        {
            if (instance.valid) instance.Remove();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
