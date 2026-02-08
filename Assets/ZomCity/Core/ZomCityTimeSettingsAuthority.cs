using UnityEngine;

namespace ZomCity
{
    /// <summary>
    /// Time 和 FramePacing 参数的单一事实源。
    /// 仅允许 Boot 和 Settings 单点入口修改。
    /// </summary>
    public static class ZomCityTimeSettingsAuthority
    {
        // MVP 阶段冻结默认值，来源于项目计划口径。
        public const float FixedDeltaTime = 1f / 60f;
        public const float MaximumDeltaTime = 1f / 15f;
        public const int VSyncCount = 0;
        public const int TargetFrameRate = 60;

        public static TimeSettingsSnapshot GetAuthoritativeSnapshot()
        {
            return new TimeSettingsSnapshot
            {
                FixedDeltaTime = FixedDeltaTime,
                MaximumDeltaTime = MaximumDeltaTime,
                VSyncCount = VSyncCount,
                TargetFrameRate = TargetFrameRate,
            };
        }

        public static void ApplyAuthoritative()
        {
            Time.fixedDeltaTime = FixedDeltaTime;
            Time.maximumDeltaTime = MaximumDeltaTime;
            QualitySettings.vSyncCount = VSyncCount;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
