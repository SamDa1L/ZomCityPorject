using System.Collections.Generic;
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

namespace ZomCity
{
    [DefaultExecutionOrder(9000)]
    [DisallowMultipleComponent]
    public sealed class GameplayPlaneConstraint : MonoBehaviour
    {
        private struct TrackedRoot
        {
            public Transform Root;
            public Rigidbody Rigid;
            public string ActorId;
        }

        private static GameplayPlaneConstraint s_instance;

        [Header("Plane Constraint")]
        public float PlaneZ = 0f;
        [Min(0f)] public float HalfThickness = 0.25f;
        public bool ClampToPlaneCenter = true;

        [Header("Auto Track")]
        public bool AutoTrackByTag = true;
        [Min(0.05f)] public float RescanInterval = 0.25f;

        [Header("Manual Track")]
        public List<Transform> ManualRoots = new List<Transform>();

        [Header("Event Cooldown")]
        [Min(1)] public int ReportCooldownFrames = 30;

        private readonly List<TrackedRoot> _trackedRoots = new List<TrackedRoot>(64);
        private readonly Dictionary<int, int> _lastReportFrameById = new Dictionary<int, int>(128);
        private float _nextRescanTime;

        public static void EnsureExists()
        {
            if (s_instance != null)
            {
                return;
            }

            s_instance = FindAnyObjectByType<GameplayPlaneConstraint>();
            if (s_instance != null)
            {
                return;
            }

            var go = new GameObject("[ZomCity]GameplayPlaneConstraint");
            s_instance = go.AddComponent<GameplayPlaneConstraint>();
            DontDestroyOnLoad(go);
        }

        public static void Register(Transform root, string actorId = null)
        {
            if (root == null)
            {
                return;
            }

            EnsureExists();
            s_instance.AddTrackedRoot(root, actorId, allowDuplicate: false);
        }

        public static void Unregister(Transform root)
        {
            if (root == null || s_instance == null)
            {
                return;
            }

            var instanceId = root.GetInstanceID();
            for (var i = s_instance._trackedRoots.Count - 1; i >= 0; i--)
            {
                if (s_instance._trackedRoots[i].Root == null)
                {
                    s_instance._trackedRoots.RemoveAt(i);
                    continue;
                }

                if (s_instance._trackedRoots[i].Root.GetInstanceID() != instanceId)
                {
                    continue;
                }

                s_instance._trackedRoots.RemoveAt(i);
            }

            s_instance._lastReportFrameById.Remove(instanceId);
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
            RebuildTrackList();
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            _trackedRoots.Clear();
            _lastReportFrameById.Clear();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextRescanTime)
            {
                _nextRescanTime = Time.unscaledTime + Mathf.Max(0.05f, RescanInterval);
                RebuildTrackList();
            }

            EnforceAll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _nextRescanTime = Time.unscaledTime;
            RebuildTrackList();
        }

        private void RebuildTrackList()
        {
            _trackedRoots.RemoveAll(item => item.Root == null);

            if (AutoTrackByTag)
            {
                var tags = TagCatalog.GameplayTags;
                for (var i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];
                    if (!TagCatalog.Exists(tag))
                    {
                        continue;
                    }

                    var tagged = GameObject.FindGameObjectsWithTag(tag);
                    for (var j = 0; j < tagged.Length; j++)
                    {
                        var go = tagged[j];
                        if (go == null)
                        {
                            continue;
                        }

                        var binding = go.GetComponent<GameplayRootBinding>();
                        if (binding != null)
                        {
                            AddTrackedRoot(binding.GetPhysicsRoot(), binding.ResolveActorId(), allowDuplicate: false);
                            continue;
                        }

                        AddTrackedRoot(go.transform, go.name, allowDuplicate: false);
                    }
                }
            }

            var bindings = FindObjectsByType<GameplayRootBinding>(FindObjectsSortMode.None);
            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                AddTrackedRoot(binding.GetPhysicsRoot(), binding.ResolveActorId(), allowDuplicate: false);
            }

            if (ManualRoots == null)
            {
                return;
            }

            for (var i = 0; i < ManualRoots.Count; i++)
            {
                var root = ManualRoots[i];
                if (root == null)
                {
                    continue;
                }

                AddTrackedRoot(root, root.name, allowDuplicate: false);
            }
        }

        private void AddTrackedRoot(Transform root, string actorId, bool allowDuplicate)
        {
            if (root == null)
            {
                return;
            }

            var instanceId = root.GetInstanceID();
            if (!allowDuplicate)
            {
                for (var i = 0; i < _trackedRoots.Count; i++)
                {
                    if (_trackedRoots[i].Root == null)
                    {
                        continue;
                    }

                    if (_trackedRoots[i].Root.GetInstanceID() == instanceId)
                    {
                        return;
                    }
                }
            }

            _trackedRoots.Add(new TrackedRoot
            {
                Root = root,
                Rigid = root.GetComponent<Rigidbody>(),
                ActorId = string.IsNullOrWhiteSpace(actorId) ? root.name : actorId,
            });
        }

        private void EnforceAll()
        {
            if (_trackedRoots.Count == 0)
            {
                return;
            }

            var minZ = PlaneZ - HalfThickness;
            var maxZ = PlaneZ + HalfThickness;

            for (var i = _trackedRoots.Count - 1; i >= 0; i--)
            {
                var item = _trackedRoots[i];
                if (item.Root == null)
                {
                    _trackedRoots.RemoveAt(i);
                    continue;
                }

                var currentPos = item.Rigid != null ? item.Rigid.position : item.Root.position;
                var observedZ = currentPos.z;

                if (observedZ >= minZ && observedZ <= maxZ)
                {
                    continue;
                }

                var correctedZ = ClampToPlaneCenter ? PlaneZ : Mathf.Clamp(observedZ, minZ, maxZ);
                if (Mathf.Approximately(observedZ, correctedZ))
                {
                    continue;
                }

                currentPos.z = correctedZ;

                if (item.Rigid != null)
                {
                    item.Rigid.position = currentPos;
                }
                else
                {
                    item.Root.position = currentPos;
                }

                PublishDriftEvent(item, observedZ, correctedZ, minZ, maxZ);
            }
        }

        private void PublishDriftEvent(TrackedRoot item, float observedZ, float correctedZ, float minZ, float maxZ)
        {
            var root = item.Root;
            if (root == null)
            {
                return;
            }

            var instanceId = root.GetInstanceID();
            var frame = Time.frameCount;

            if (_lastReportFrameById.TryGetValue(instanceId, out var lastFrame))
            {
                if (frame - lastFrame < Mathf.Max(1, ReportCooldownFrames))
                {
                    return;
                }
            }

            _lastReportFrameById[instanceId] = frame;

            GameplayEventHub.Publish(new PlaneDriftEvent
            {
                EventId = GameplayEventIds.PlaneDrift,
                Frame = frame,
                Scene = SceneManager.GetActiveScene().name,
                ActorId = string.IsNullOrWhiteSpace(item.ActorId) ? root.name : item.ActorId,
                ObservedZ = observedZ,
                CorrectedZ = correctedZ,
                MinAllowedZ = minZ,
                MaxAllowedZ = maxZ,
            });
        }
    }
}
