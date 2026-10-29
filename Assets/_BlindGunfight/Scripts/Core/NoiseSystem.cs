using System;
using UnityEngine;

namespace BlindGunfight
{
    /// <summary>
    /// 총소리, 발소리, 비명 같은 소음을 알린다. 적 AI의 Perception 이 듣는다.
    /// </summary>
    public static class NoiseSystem
    {
        /// <summary>위치, 들리는 반경(m), 소리를 낸 오브젝트</summary>
        public static event Action<Vector3, float, GameObject> NoiseMade;

        public static void Emit(Vector3 position, float radius, GameObject source)
        {
            if (radius <= 0f) return;
            NoiseMade?.Invoke(position, radius, source);
        }

        // Enter Play Mode 옵션에서 도메인 리로드를 꺼도 이벤트가 쌓이지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            NoiseMade = null;
        }
    }
}
