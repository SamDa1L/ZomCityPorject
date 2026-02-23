using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ZomCity
{
    public static class AddressablesLoader
    {
        private const string AddressablesTypeName = "UnityEngine.AddressableAssets.Addressables, Unity.Addressables";

        private static Type s_addressablesType;
        private static MethodInfo s_loadAssetMethod;

        public static bool IsAddressablesAvailable => ResolveAddressablesType() != null;

        public static IEnumerator LoadPrefabAsync(string prefabKey, Action<GameObject> onLoaded, Action<string> onFailed)
        {
            if (!PrefabKeyValidator.TryValidate(prefabKey, out var reason))
            {
                ReportFailed(prefabKey, reason, onFailed);
                yield break;
            }

            var addressablesType = ResolveAddressablesType();
            if (addressablesType != null)
            {
                var method = ResolveLoadAssetMethod(addressablesType);
                if (method == null)
                {
                    ReportFailed(prefabKey, "Addressables operation failed", onFailed);
                    yield break;
                }

                var genericMethod = method.MakeGenericMethod(typeof(GameObject));
                object handle;

                try
                {
                    handle = genericMethod.Invoke(null, new object[] { prefabKey });
                }
                catch (Exception ex)
                {
                    ReportFailed(prefabKey, "Addressables operation failed" + ex.Message, onFailed);
                    yield break;
                }

                if (handle == null)
                {
                    ReportFailed(prefabKey, "Addressables operation failed", onFailed);
                    yield break;
                }

                var handleType = handle.GetType();
                var isDoneProp = handleType.GetProperty("IsDone");
                var statusProp = handleType.GetProperty("Status");
                var resultProp = handleType.GetProperty("Result");

                if (isDoneProp == null || statusProp == null || resultProp == null)
                {
                    ReportFailed(prefabKey, "Addressables operation failed", onFailed);
                    yield break;
                }

                while (!(bool)isDoneProp.GetValue(handle))
                {
                    yield return null;
                }

                var status = statusProp.GetValue(handle);
                if (status != null && string.Equals(status.ToString(), "Succeeded", StringComparison.Ordinal))
                {
                    var prefab = resultProp.GetValue(handle) as GameObject;
                    if (prefab != null)
                    {
                        onLoaded?.Invoke(prefab);
                        yield break;
                    }

                    ReportFailed(prefabKey, "Addressables operation failed", onFailed);
                    yield break;
                }

                ReportFailed(prefabKey, "Addressables operation failed", onFailed);
                yield break;
            }

            if (DataRegistry.TryGetAddressFallback(prefabKey, out var resourcePath))
            {
                var prefab = Resources.Load<GameObject>(resourcePath);
                if (prefab != null)
                {
                    onLoaded?.Invoke(prefab);
                    yield break;
                }

                ReportFailed(prefabKey, "Resources fallback load failed: " + resourcePath, onFailed);
                yield break;
            }

            ReportFailed(prefabKey, "Addressables operation failed", onFailed);
        }

        private static Type ResolveAddressablesType()
        {
            if (s_addressablesType != null)
            {
                return s_addressablesType;
            }

            s_addressablesType = Type.GetType(AddressablesTypeName);
            return s_addressablesType;
        }

        private static MethodInfo ResolveLoadAssetMethod(Type addressablesType)
        {
            if (s_loadAssetMethod != null)
            {
                return s_loadAssetMethod;
            }

            var methods = addressablesType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (!string.Equals(method.Name, "LoadAssetAsync", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!method.IsGenericMethodDefinition)
                {
                    continue;
                }

                var args = method.GetParameters();
                if (args.Length != 1)
                {
                    continue;
                }

                if (args[0].ParameterType != typeof(string))
                {
                    continue;
                }

                s_loadAssetMethod = method;
                return s_loadAssetMethod;
            }

            return null;
        }

        private static void ReportFailed(string prefabKey, string reason, Action<string> onFailed)
        {
            DataRegistry.PublishPrefabLoadFailed(prefabKey, reason);
            onFailed?.Invoke(reason);
        }
    }
}
