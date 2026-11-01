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

        /// <summary>
        /// 낮게 쏘기(Ctrl)와 상대 자세까지 따진 판정.
        /// 서 있는 적: 보통은 머리까지, 낮게 쏘면 다리(팔다리/스침)만 맞는다.
        /// 앉은 적: 보통으로 쏘면 머리 위로 지나가 몸통/스침, 낮게 쏴야 머리까지 맞는다.
        /// </summary>
        public static HitZone Evaluate(float offsetRatio, bool lowAim, bool targetCrouching)
        {
            if (lowAim == targetCrouching) return Evaluate(offsetRatio);
            if (lowAim) return offsetRatio <= BodyMaxRatio ? HitZone.Limb : HitZone.Graze;
            return offsetRatio <= BodyMaxRatio ? HitZone.Body : HitZone.Graze;
        }

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
