namespace BlindGunfight
{
    public enum HitZone
    {
        Head,
        Body,
        Limb,
        Graze,
    }

    /// <summary>
    /// 탑다운 명중 부위 판정. 탄이 적 몸 중심에서 얼마나 떨어져 지나갔는지(반지름 대비 비율)로 정한다.
    /// 위에서 보면 머리가 정중앙, 그 바깥이 몸통, 가장자리가 팔다리, 맨 끝은 스침이다.
    /// </summary>
    public static class HitZones
    {
        // 반지름 대비 비율 경계. 이 값들을 바꿔 부위 크기를 조정한다.
        public const float HeadMaxRatio = 0.25f;
        public const float BodyMaxRatio = 0.6f;
        public const float LimbMaxRatio = 0.85f;

        public static HitZone Evaluate(float offsetRatio)
        {
            if (offsetRatio <= HeadMaxRatio) return HitZone.Head;
            if (offsetRatio <= BodyMaxRatio) return HitZone.Body;
            if (offsetRatio <= LimbMaxRatio) return HitZone.Limb;
            return HitZone.Graze;
        }

        public static float DamageMultiplier(HitZone zone)
        {
            switch (zone)
            {
                case HitZone.Head: return 2f;
                case HitZone.Body: return 1f;
                case HitZone.Limb: return 0.6f;
                default: return 0.3f;
            }
        }
    }
}
