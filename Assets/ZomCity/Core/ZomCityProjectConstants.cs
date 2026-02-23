using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    /// <summary>
    /// 项目路径与跨系统合同的“单一事实源”（常量集中管理）。
    /// 工具链与运行时代码必须引用这里，禁止散落硬编码字符串。
    /// </summary>
    public static class ZomCityProjectConstants
    {
        public static class Paths
        {
            // 统一使用“工程根目录相对路径”（禁止硬编码绝对路径）。
            public const string DocsRoot = "Docs/ZomCity/";
            public const string ReportsOutDir = "TempLogs/ZomCityReports/";

            public const string DesignRoot = "Assets/ZomCity/Data/Design/";
            public const string GeneratedRoot = "Assets/ZomCity/Data/Generated/";
            public const string ContentRoot = "Assets/ZomCity/Content/";
        }

        public static class Reports
        {
            public const string TimestampFormat = "yyyyMMdd_HHmmss";
            public const int Retention = 20;

            public static string FormatJson(string reportName, DateTime utcNow)
                => $"{reportName}_{utcNow:yyyyMMdd_HHmmss}.json";
        }

        public static class Addressables
        {
            public const string AddressPrefix = "ZC";

            public static readonly string[] DomainWhitelist =
            {
                "Enemies",
                "Rooms",
                "Containers",
                "Items",
                "UI",
                "VFX",
                "Audio",
            };

            public static string MakeAddress(string domain, string stableId)
                => $"{AddressPrefix}/{domain}/{stableId}";
        }

        public static class UnityTags
        {
            public static readonly string[] Whitelist =
            {
                "Player",
                "Enemy",
                "Interactable",
                "Container",
            };
        }

        public static class StableIds
        {
            public static readonly string[] PrefixWhitelist =
            {
                "MAT_",
                "CON_",
                "WPN_",
                "ATT_",
                "DATA_",
                "LT_",
                "CT_",
                "EN_",
                "RM_",
                "ZN_",
                "RC_",
                "UP_",
            };
        }
    }
}

namespace ZomCity
{
    public static class LayerCatalog
    {
        public const string Default = "Default";
        public const string Ground = "Ground";
        public const string OneWayPlatform = "OneWayPlatform";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Interactable = "Interactable";
        public const string Container = "Container";
        public const string Projectile = "Projectile";

        private static readonly string[] RequiredLayers =
        {
            Ground,
            OneWayPlatform,
            Player,
            Enemy,
            Interactable,
            Container,
            Projectile,
        };

        public static int Resolve(string layerName)
        {
            return LayerMask.NameToLayer(layerName);
        }

        public static LayerMask TryBuildMask(params string[] layerNames)
        {
            var bits = 0;
            if (layerNames == null)
            {
                return bits;
            }

            for (var i = 0; i < layerNames.Length; i++)
            {
                var layerName = layerNames[i];
                if (string.IsNullOrWhiteSpace(layerName))
                {
                    continue;
                }

                var layerIndex = Resolve(layerName);
                if (layerIndex < 0)
                {
                    continue;
                }

                bits |= 1 << layerIndex;
            }

            return bits;
        }

        public static LayerMask CombatHitMask
        {
            get
            {
                var mask = TryBuildMask(
                    Enemy,
                    Interactable,
                    Container,
                    Ground,
                    OneWayPlatform,
                    Default);

                if (mask.value != 0)
                {
                    return mask;
                }

                return ~0;
            }
        }


        public static LayerMask GroundMask => TryBuildMask(Ground, OneWayPlatform);

        public static LayerMask InteractableMask => TryBuildMask(Interactable, Container);

        public static LayerMask ActorMask => TryBuildMask(Player, Enemy, Projectile);

        public static LayerMask GameplayQueryMask
        {
            get
            {
                var mask = TryBuildMask(
                    Player,
                    Enemy,
                    Projectile,
                    Interactable,
                    Container,
                    Ground,
                    OneWayPlatform,
                    Default);

                if (mask.value != 0)
                {
                    return mask;
                }

                return ~0;
            }
        }

        public static string[] CollectMissingRequiredLayers()
        {
            var missing = new List<string>();

            for (var i = 0; i < RequiredLayers.Length; i++)
            {
                var layerName = RequiredLayers[i];
                if (Resolve(layerName) >= 0)
                {
                    continue;
                }

                missing.Add(layerName);
            }

            return missing.ToArray();
        }
    }

    public static class TagCatalog
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Interactable = "Interactable";
        public const string Container = "Container";

        private static readonly string[] RequiredTags =
        {
            Player,
            Enemy,
            Interactable,
            Container,
        };

        public static IReadOnlyList<string> GameplayTags => RequiredTags;

        public static bool Exists(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

#if UNITY_EDITOR
            var editorTags = UnityEditorInternal.InternalEditorUtility.tags;
            for (var i = 0; i < editorTags.Length; i++)
            {
                if (string.Equals(editorTags[i], tagName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
#else
            try
            {
                _ = GameObject.FindGameObjectWithTag(tagName);
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
#endif
        }

        public static bool IsAllowedGameplayTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

            for (var i = 0; i < RequiredTags.Length; i++)
            {
                if (string.Equals(RequiredTags[i], tagName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static string[] CollectMissingRequiredTags()
        {
            var missing = new List<string>();

            for (var i = 0; i < RequiredTags.Length; i++)
            {
                var tagName = RequiredTags[i];
                if (Exists(tagName))
                {
                    continue;
                }

                missing.Add(tagName);
            }

            return missing.ToArray();
        }
    }

    [DefaultExecutionOrder(-12000)]
    [DisallowMultipleComponent]
    public sealed class LayerTagCatalogGuard : MonoBehaviour
    {
        private static LayerTagCatalogGuard s_instance;

        [Min(0.5f)] public float RecheckInterval = 2f;
        public bool LogPassMessage = true;

        private float _nextCheckTime;
        private string _lastSignature = string.Empty;
        private Coroutine _deferredValidateRoutine;

        public static void EnsureExists()
        {
            if (s_instance != null)
            {
                return;
            }

            s_instance = FindAnyObjectByType<LayerTagCatalogGuard>();
            if (s_instance != null)
            {
                return;
            }

            var go = new GameObject("[ZomCity]LayerTagCatalogGuard");
            s_instance = go.AddComponent<LayerTagCatalogGuard>();
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
            SceneManager.sceneLoaded += OnSceneLoaded;
            ScheduleValidate(forceLog: true);
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }

            if (_deferredValidateRoutine != null)
            {
                StopCoroutine(_deferredValidateRoutine);
                _deferredValidateRoutine = null;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }

            if (Time.unscaledTime < _nextCheckTime)
            {
                return;
            }

            _nextCheckTime = Time.unscaledTime + Mathf.Max(0.5f, RecheckInterval);
            ScheduleValidate(forceLog: false);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _nextCheckTime = Time.unscaledTime;
            ScheduleValidate(forceLog: true);
        }

        private void ScheduleValidate(bool forceLog)
        {
            if (!isActiveAndEnabled)
            {
                ValidateNow(forceLog);
                return;
            }

            if (_deferredValidateRoutine != null)
            {
                StopCoroutine(_deferredValidateRoutine);
            }

            _deferredValidateRoutine = StartCoroutine(ValidateNextFrame(forceLog));
        }

        private IEnumerator ValidateNextFrame(bool forceLog)
        {
            // 延后一帧再做 Catalog 校验，避开场景恢复阶段。
            yield return null;

            _deferredValidateRoutine = null;
            ValidateNow(forceLog);
        }

        private void ValidateNow(bool forceLog = false)
        {
            var missingLayers = LayerCatalog.CollectMissingRequiredLayers();
            var missingTags = TagCatalog.CollectMissingRequiredTags();

            var layerText = JoinOrNone(missingLayers);
            var tagText = JoinOrNone(missingTags);
            var signature = layerText + "|" + tagText;

            if (!forceLog && string.Equals(_lastSignature, signature, StringComparison.Ordinal))
            {
                return;
            }

            _lastSignature = signature;

            if (missingLayers.Length == 0 && missingTags.Length == 0)
            {
                if (LogPassMessage)
                {
                    Debug.Log("[ZomCity] LayerTagCatalog check passed");
                }

                return;
            }

            Debug.LogWarning(
                "[ZomCity] LayerTagCatalog missing items " +
                "Layers=" + layerText + " " +
                "Tags=" + tagText);
        }

        private static string JoinOrNone(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "none";
            }

            return string.Join(",", values);
        }
    }
}

