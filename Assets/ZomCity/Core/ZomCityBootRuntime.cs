using UnityEngine;

namespace ZomCity
{
    /// <summary>
    /// 纯代码 Boot 钩子（M0.1）。
    /// 启动时冻结 Time 和 FramePacing 参数并创建 Guard。
    /// </summary>
    public static class ZomCityBootRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            ZomCityTimeSettingsAuthority.ApplyAuthoritative();
            TimeSettingsGuard.EnsureExists();
        }
    }
}
