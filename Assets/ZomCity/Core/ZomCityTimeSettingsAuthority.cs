using UnityEngine;

namespace ZomCity
{
    /// <summary>
    /// Single source of truth for time / frame pacing settings (M0).
    /// Only Boot/Settings should apply changes. Any runtime tampering is guarded.
    /// </summary>
    public static class ZomCityTimeSettingsAuthority
    {
        // MVP frozen defaults (from ZomCity plan).
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

