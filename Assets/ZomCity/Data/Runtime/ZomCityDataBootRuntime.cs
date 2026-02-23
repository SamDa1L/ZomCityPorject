using UnityEngine;

namespace ZomCity
{
    public static class ZomCityDataBootRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            DataRegistry.EnsureLoaded();
        }
    }
}
