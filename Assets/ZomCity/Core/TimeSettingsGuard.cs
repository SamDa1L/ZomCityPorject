using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    /// <summary>
    /// 监测并回写运行时被篡改的时间和帧率步进参数。
    /// 这些参数在 M0 阶段为冻结状态。
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

            // 启动时先应用一次，确保单一权威入口生效。
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

            // 无论是否为发布构建都要回写，避免长期抖动和物理步进异常。
            ZomCityTimeSettingsAuthority.ApplyAuthoritative();

            // 仅在开发构建上报事件，发布构建只回写不刷日志。
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
