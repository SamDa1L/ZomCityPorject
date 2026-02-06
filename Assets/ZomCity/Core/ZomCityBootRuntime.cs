using UnityEngine;

namespace ZomCity
{
    /// <summary>
    /// Code-only boot hook for M0.1 to freeze time/frame pacing and spawn the guard.
    /// A dedicated Boot scene/state machine will be layered in M0+/M1.
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

