using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    /// <summary>
    /// Detects and restores any runtime tampering of the frozen time/frame pacing knobs.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class TimeSettingsGuard : MonoBehaviour
    {
        private const float FloatEpsilon = 0.00001f;

        private static TimeSettingsGuard s_instance;

        public static void EnsureExists()
        {
            if (s_instance != null)
            {
                return;
            }

            s_instance = FindAnyObjectByType<TimeSettingsGuard>();
            if (s_instance != null)
            {
                return;
            }

            var go = new GameObject("[ZomCity]TimeSettingsGuard");
            s_instance = go.AddComponent<TimeSettingsGuard>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);

            // Apply once on startup to ensure a single authoritative entry point.
            ZomCityTimeSettingsAuthority.ApplyAuthoritative();
        }

        private void LateUpdate()
        {
            Enforce();
        }

        private static TimeSettingsSnapshot CaptureObservedSnapshot()
        {
            return new TimeSettingsSnapshot
            {
                FixedDeltaTime = Time.fixedDeltaTime,
                MaximumDeltaTime = Time.maximumDeltaTime,
                VSyncCount = QualitySettings.vSyncCount,
                TargetFrameRate = Application.targetFrameRate,
            };
        }

        private static bool IsDifferent(TimeSettingsSnapshot a, TimeSettingsSnapshot b)
        {
            if (Mathf.Abs(a.FixedDeltaTime - b.FixedDeltaTime) > FloatEpsilon) return true;
            if (Mathf.Abs(a.MaximumDeltaTime - b.MaximumDeltaTime) > FloatEpsilon) return true;
            if (a.VSyncCount != b.VSyncCount) return true;
            if (a.TargetFrameRate != b.TargetFrameRate) return true;
            return false;
        }

        private void Enforce()
        {
            var authoritative = ZomCityTimeSettingsAuthority.GetAuthoritativeSnapshot();
            var observed = CaptureObservedSnapshot();

            if (!IsDifferent(observed, authoritative))
            {
                return;
            }

            // Always restore (even in release builds) to avoid long-tail jitter / physics issues.
            ZomCityTimeSettingsAuthority.ApplyAuthoritative();

            // Report only in development contexts.
            if (!Debug.isDebugBuild)
            {
                return;
            }

            GameplayEventHub.Publish(new TimeTamperedEvent
            {
                EventId = GameplayEventIds.TimeTampered,
                Frame = Time.frameCount,
                Scene = SceneManager.GetActiveScene().name,
                Observed = observed,
                Authoritative = authoritative,
            });
        }
    }
}

