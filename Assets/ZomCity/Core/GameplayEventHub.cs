using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    /// <summary>
    /// 最小事件总线（M0），用于开发期可观测性。
    /// </summary>
    public static class GameplayEventHub
    {
        public static event Action<TimeTamperedEvent> TimeTampered;
        public static event Action<CameraTargetLostEvent> CameraTargetLost;
        public static event Action<FireRequestedEvent> CombatFireRequested;
        public static event Action<FireConsumedEvent> CombatFireConsumed;
        public static event Action<FireBlockedEvent> CombatFireBlocked;
        public static event Action<PlaneDriftEvent> PlaneDrift;
        public static event Action<DataMissingEvent> DataMissing;
        public static event Action<PrefabLoadFailedEvent> PrefabLoadFailed;
        public static event Action<NoiseEvent> Noise;
        public static event Action<DamageEvent> CombatDamage;
        public static event Action<PickupEvent> LootPickup;

        public static void Publish(TimeTamperedEvent evt)
        {
            TimeTampered?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} " +
                    $"fixedDT {evt.Observed.FixedDeltaTime:F6}->{evt.Authoritative.FixedDeltaTime:F6}, " +
                    $"maxDT {evt.Observed.MaximumDeltaTime:F6}->{evt.Authoritative.MaximumDeltaTime:F6}, " +
                    $"vSync {evt.Observed.VSyncCount}->{evt.Authoritative.VSyncCount}, " +
                    $"targetFPS {evt.Observed.TargetFrameRate}->{evt.Authoritative.TargetFrameRate}");
            }
        }

        public static void Publish(CameraTargetLostEvent evt)
        {
            CameraTargetLost?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId}");
            }
        }

        public static void Publish(FireRequestedEvent evt)
        {
            CombatFireRequested?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"screen=({evt.ScreenPosition.x:F1},{evt.ScreenPosition.y:F1})");
            }
        }

        public static void Publish(FireBlockedEvent evt)
        {
            CombatFireBlocked?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} reason={evt.Reason}");
            }
        }

        public static void Publish(FireConsumedEvent evt)
        {
            CombatFireConsumed?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"requestFrame={evt.RequestFrame} aim=({evt.AimPoint.x:F2},{evt.AimPoint.y:F2},{evt.AimPoint.z:F2}) hit={evt.HasHit}");
            }
        }

        public static void Publish(PlaneDriftEvent evt)
        {
            PlaneDrift?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"z {evt.ObservedZ:F3}->{evt.CorrectedZ:F3} range=[{evt.MinAllowedZ:F3},{evt.MaxAllowedZ:F3}]");
            }
        }

        public static void Publish(DataMissingEvent evt)
        {
            DataMissing?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogError(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"domain={evt.DataDomain} id={evt.DataId} dataVersion={evt.DataVersion} saveVersion={evt.SaveVersion} reason={evt.Reason}");
            }
        }

        public static void Publish(PrefabLoadFailedEvent evt)
        {
            PrefabLoadFailed?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogError(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"prefabKey={evt.PrefabKey} dataVersion={evt.DataVersion} saveVersion={evt.SaveVersion} reason={evt.Reason}");
            }
        }

        public static void Publish(NoiseEvent evt)
        {
            Noise?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"weapon={evt.WeaponId} radius={evt.Radius:F2} source=({evt.SourcePosition.x:F2},{evt.SourcePosition.y:F2},{evt.SourcePosition.z:F2}) reason={evt.Reason}");
            }
        }

        public static void Publish(DamageEvent evt)
        {
            CombatDamage?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"weapon={evt.WeaponId} target={evt.TargetId} amount={evt.Amount:F2} " +
                    $"hit=({evt.HitPoint.x:F2},{evt.HitPoint.y:F2},{evt.HitPoint.z:F2})");
            }
        }

        public static void Publish(PickupEvent evt)
        {
            LootPickup?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"item={evt.ItemId} qty={evt.Quantity} source={evt.Source}");
            }
        }
    }

    public static class ZomCityHotkeyInput
    {
#if ENABLE_INPUT_SYSTEM
        private const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags StaticPublic = BindingFlags.Static | BindingFlags.Public;

        private static Type s_keyboardType;
        private static PropertyInfo s_keyboardCurrentProperty;
#endif

        public static bool ReadKeyDown(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            return ReadKeyDownWithInputSystem(keyCode);
#else
            return Input.GetKeyDown(keyCode);
#endif
        }

        public static bool ReadCtrlModifiedKeyDown(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            if (!ReadKeyBoolWithInputSystem(keyCode, "wasPressedThisFrame"))
            {
                return false;
            }

            return ReadKeyboardKeyBool("leftCtrlKey", "isPressed") || ReadKeyboardKeyBool("rightCtrlKey", "isPressed");
#else
            if (!Input.GetKeyDown(keyCode))
            {
                return false;
            }

            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool ReadKeyDownWithInputSystem(KeyCode keyCode)
        {
            return ReadKeyBoolWithInputSystem(keyCode, "wasPressedThisFrame");
        }

        private static bool ReadKeyBoolWithInputSystem(KeyCode keyCode, string boolPropertyName)
        {
            if (!TryMapKeyCodeToKeyboardProperty(keyCode, out var keyPropertyName))
            {
                return false;
            }

            return ReadKeyboardKeyBool(keyPropertyName, boolPropertyName);
        }

        private static bool ReadKeyboardKeyBool(string keyPropertyName, string boolPropertyName)
        {
            var keyboard = GetKeyboardInstance();
            if (keyboard == null)
            {
                return false;
            }

            var keyProperty = keyboard.GetType().GetProperty(keyPropertyName, InstancePublic);
            if (keyProperty == null)
            {
                return false;
            }

            var keyControl = keyProperty.GetValue(keyboard, null);
            if (keyControl == null)
            {
                return false;
            }

            var valueProperty = keyControl.GetType().GetProperty(boolPropertyName, InstancePublic);
            if (valueProperty == null)
            {
                return false;
            }

            var raw = valueProperty.GetValue(keyControl, null);
            return raw is bool pressed && pressed;
        }

        private static object GetKeyboardInstance()
        {
            if (!EnsureKeyboardType())
            {
                return null;
            }

            return s_keyboardCurrentProperty.GetValue(null, null);
        }

        private static bool EnsureKeyboardType()
        {
            if (s_keyboardType != null && s_keyboardCurrentProperty != null)
            {
                return true;
            }

            s_keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            if (s_keyboardType == null)
            {
                return false;
            }

            s_keyboardCurrentProperty = s_keyboardType.GetProperty("current", StaticPublic);
            return s_keyboardCurrentProperty != null;
        }
#endif

        private static bool TryMapKeyCodeToKeyboardProperty(KeyCode keyCode, out string propertyName)
        {
            switch (keyCode)
            {
                case KeyCode.F1:
                    propertyName = "f1Key";
                    return true;
                case KeyCode.F2:
                    propertyName = "f2Key";
                    return true;
                case KeyCode.F3:
                    propertyName = "f3Key";
                    return true;
                case KeyCode.F4:
                    propertyName = "f4Key";
                    return true;
                case KeyCode.F5:
                    propertyName = "f5Key";
                    return true;
                case KeyCode.F6:
                    propertyName = "f6Key";
                    return true;
                case KeyCode.F7:
                    propertyName = "f7Key";
                    return true;
                case KeyCode.F8:
                    propertyName = "f8Key";
                    return true;
                case KeyCode.F9:
                    propertyName = "f9Key";
                    return true;
                case KeyCode.F10:
                    propertyName = "f10Key";
                    return true;
                case KeyCode.F11:
                    propertyName = "f11Key";
                    return true;
                case KeyCode.F12:
                    propertyName = "f12Key";
                    return true;
                case KeyCode.Escape:
                    propertyName = "escapeKey";
                    return true;
                case KeyCode.BackQuote:
                    propertyName = "backquoteKey";
                    return true;
                case KeyCode.Tab:
                    propertyName = "tabKey";
                    return true;
                case KeyCode.I:
                    propertyName = "iKey";
                    return true;
                default:
                    propertyName = null;
                    return false;
            }
        }
    }

    [DefaultExecutionOrder(9900)]
    [DisallowMultipleComponent]
    public sealed class GameplayEventViewer : MonoBehaviour
    {
        [Serializable]
        public sealed class GameplayEventLogRecord
        {
            public string TimestampUtc;
            public string EventId;
            public string Domain;
            public int Frame;
            public string Scene;
            public string ActorId;
            public string ItemId;
            public string WeaponId;
            public string Summary;
            public string PayloadJson;
        }

        [Serializable]
        private sealed class GameplayEventViewerExport
        {
            public string GeneratedAtUtc;
            public int Total;
            public GameplayEventLogRecord[] Records;
        }

        private static GameplayEventViewer s_instance;

        [Header("窗口")]
        public bool Visible = true;
        public KeyCode ToggleKey = KeyCode.F2;
        public Rect WindowRect = new Rect(544f, 16f, 780f, 620f);
        [Min(20)] public int MaxRecords = 200;
        public bool PauseUpdate;
        public bool AutoScroll = true;
        public bool OnlyCurrentScene;

        [Header("过滤")]
        public string DomainFilter = string.Empty;
        public string EventFilter = string.Empty;
        public string ActorFilter = string.Empty;

        [Header("运行时统计")]
        [SerializeField] private int totalReceived;
        [SerializeField] private int filteredVisible;

        private readonly List<GameplayEventLogRecord> _records = new List<GameplayEventLogRecord>(256);
        private Vector2 _scroll;
        private bool _subscribed;

        public static GameplayEventViewer Instance => s_instance;

        public int CachedRecordCount => _records.Count;
        public int TotalReceivedCount => totalReceived;

        public void ClearRecords()
        {
            _records.Clear();
            filteredVisible = 0;
        }

        public bool TryGetLastRecord(out GameplayEventLogRecord record)
        {
            if (_records.Count == 0)
            {
                record = null;
                return false;
            }

            record = _records[_records.Count - 1];
            return record != null;
        }

        public bool ContainsRecord(string eventId, string itemId = null, string weaponId = null)
        {
            for (var i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (record == null)
                {
                    continue;
                }

                if (!string.Equals(record.EventId, eventId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(itemId) && !string.Equals(record.ItemId, itemId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(weaponId) && !string.Equals(record.WeaponId, weaponId, StringComparison.Ordinal))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public static void EnsureExists()
        {
            if (s_instance != null)
            {
                return;
            }

            s_instance = FindAnyObjectByType<GameplayEventViewer>();
            if (s_instance != null)
            {
                return;
            }

            var go = new GameObject("[ZomCity]GameplayEventViewer");
            s_instance = go.AddComponent<GameplayEventViewer>();
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
            SubscribeEvents();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void Update()
        {
            if (ZomCityHotkeyInput.ReadKeyDown(ToggleKey))
            {
                Visible = !Visible;
            }
        }

        private void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, "ZomCity EventViewer");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"Total={totalReceived} Cached={_records.Count} Visible={filteredVisible}");

            GUILayout.BeginHorizontal();
            PauseUpdate = GUILayout.Toggle(PauseUpdate, "暂停更新");
            AutoScroll = GUILayout.Toggle(AutoScroll, "自动滚动");
            OnlyCurrentScene = GUILayout.Toggle(OnlyCurrentScene, "仅当前场景");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("清空"))
            {
                _records.Clear();
                filteredVisible = 0;
            }

            if (GUILayout.Button("导出JSON"))
            {
                ExportSnapshot();
            }

            if (GUILayout.Button("复制最后一条JSON"))
            {
                CopyLastPayloadJson();
            }
            GUILayout.EndHorizontal();

            MaxRecords = Mathf.RoundToInt(GUILayout.HorizontalSlider(MaxRecords, 20f, 1000f));
            GUILayout.Label($"缓存上限: {MaxRecords}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Domain", GUILayout.Width(54f));
            DomainFilter = GUILayout.TextField(DomainFilter ?? string.Empty, GUILayout.Width(140f));
            GUILayout.Label("Event", GUILayout.Width(44f));
            EventFilter = GUILayout.TextField(EventFilter ?? string.Empty, GUILayout.Width(180f));
            GUILayout.Label("Actor", GUILayout.Width(44f));
            ActorFilter = GUILayout.TextField(ActorFilter ?? string.Empty, GUILayout.Width(140f));
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(430f));
            var visible = 0;
            for (var i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                if (!PassFilter(record))
                {
                    continue;
                }

                visible++;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("复制", GUILayout.Width(44f)))
                {
                    GUIUtility.systemCopyBuffer = string.IsNullOrWhiteSpace(record.PayloadJson)
                        ? record.Summary
                        : record.PayloadJson;
                }

                GUILayout.Label(
                    $"[{record.Frame}] {record.EventId} actor={record.ActorId} item={record.ItemId} weapon={record.WeaponId} {record.Summary}",
                    GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }

            filteredVisible = visible;
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUI.DragWindow();

            if (AutoScroll && Event.current.type == EventType.Repaint)
            {
                _scroll.y = 999999f;
            }
        }

        private void SubscribeEvents()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            GameplayEventHub.TimeTampered += OnTimeTampered;
            GameplayEventHub.CameraTargetLost += OnCameraTargetLost;
            GameplayEventHub.CombatFireRequested += OnFireRequested;
            GameplayEventHub.CombatFireConsumed += OnFireConsumed;
            GameplayEventHub.CombatFireBlocked += OnFireBlocked;
            GameplayEventHub.PlaneDrift += OnPlaneDrift;
            GameplayEventHub.DataMissing += OnDataMissing;
            GameplayEventHub.PrefabLoadFailed += OnPrefabLoadFailed;
            GameplayEventHub.Noise += OnNoise;
            GameplayEventHub.CombatDamage += OnDamage;
            GameplayEventHub.LootPickup += OnPickup;
        }

        private void UnsubscribeEvents()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            GameplayEventHub.TimeTampered -= OnTimeTampered;
            GameplayEventHub.CameraTargetLost -= OnCameraTargetLost;
            GameplayEventHub.CombatFireRequested -= OnFireRequested;
            GameplayEventHub.CombatFireConsumed -= OnFireConsumed;
            GameplayEventHub.CombatFireBlocked -= OnFireBlocked;
            GameplayEventHub.PlaneDrift -= OnPlaneDrift;
            GameplayEventHub.DataMissing -= OnDataMissing;
            GameplayEventHub.PrefabLoadFailed -= OnPrefabLoadFailed;
            GameplayEventHub.Noise -= OnNoise;
            GameplayEventHub.CombatDamage -= OnDamage;
            GameplayEventHub.LootPickup -= OnPickup;
        }

        private void OnTimeTampered(TimeTamperedEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, "SYSTEM_TIME", string.Empty, string.Empty,
                $"targetFPS={evt.Observed.TargetFrameRate}->{evt.Authoritative.TargetFrameRate}",
                JsonUtility.ToJson(evt));
        }

        private void OnCameraTargetLost(CameraTargetLostEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, string.Empty, string.Empty,
                "camera target lost", JsonUtility.ToJson(evt));
        }

        private void OnFireRequested(FireRequestedEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, string.Empty, string.Empty,
                $"screen=({evt.ScreenPosition.x:F1},{evt.ScreenPosition.y:F1})", JsonUtility.ToJson(evt));
        }

        private void OnFireConsumed(FireConsumedEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, string.Empty, string.Empty,
                $"requestFrame={evt.RequestFrame} hit={evt.HasHit}", JsonUtility.ToJson(evt));
        }

        private void OnFireBlocked(FireBlockedEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, string.Empty, string.Empty,
                $"reason={evt.Reason}", JsonUtility.ToJson(evt));
        }

        private void OnPlaneDrift(PlaneDriftEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, string.Empty, string.Empty,
                $"z={evt.ObservedZ:F3}->{evt.CorrectedZ:F3}", JsonUtility.ToJson(evt));
        }

        private void OnDataMissing(DataMissingEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, evt.DataId, string.Empty,
                $"domain={evt.DataDomain}", JsonUtility.ToJson(evt));
        }

        private void OnPrefabLoadFailed(PrefabLoadFailedEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, evt.PrefabKey, string.Empty,
                $"reason={evt.Reason}", JsonUtility.ToJson(evt));
        }

        private void OnNoise(NoiseEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, string.Empty, evt.WeaponId,
                $"radius={evt.Radius:F2} reason={evt.Reason}", JsonUtility.ToJson(evt));
        }

        private void OnDamage(DamageEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, evt.TargetId, evt.WeaponId,
                $"amount={evt.Amount:F2}", JsonUtility.ToJson(evt));
        }

        private void OnPickup(PickupEvent evt)
        {
            AddRecord(evt.EventId, evt.Frame, evt.Scene, evt.ActorId, evt.ItemId, string.Empty,
                $"qty={evt.Quantity} source={evt.Source}", JsonUtility.ToJson(evt));
        }

        private void AddRecord(
            string eventId,
            int frame,
            string scene,
            string actorId,
            string itemId,
            string weaponId,
            string summary,
            string payloadJson)
        {
            totalReceived++;

            if (PauseUpdate)
            {
                return;
            }

            _records.Add(new GameplayEventLogRecord
            {
                TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EventId = eventId,
                Domain = ResolveDomain(eventId),
                Frame = frame,
                Scene = scene,
                ActorId = actorId,
                ItemId = itemId,
                WeaponId = weaponId,
                Summary = summary,
                PayloadJson = payloadJson,
            });

            var keep = Mathf.Max(20, MaxRecords);
            while (_records.Count > keep)
            {
                _records.RemoveAt(0);
            }
        }

        private bool PassFilter(GameplayEventLogRecord record)
        {
            if (record == null)
            {
                return false;
            }

            if (OnlyCurrentScene)
            {
                var currentScene = SceneManager.GetActiveScene().name;
                if (!string.Equals(record.Scene, currentScene, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            if (!ContainsIgnoreCase(record.Domain, DomainFilter))
            {
                return false;
            }

            if (!ContainsIgnoreCase(record.EventId, EventFilter))
            {
                return false;
            }

            if (!ContainsIgnoreCase(record.ActorId, ActorFilter))
            {
                return false;
            }

            return true;
        }

        private static bool ContainsIgnoreCase(string source, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            return source.IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveDomain(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return "Unknown";
            }

            var first = eventId.IndexOf('_');
            if (first < 0)
            {
                return "Unknown";
            }

            var second = eventId.IndexOf('_', first + 1);
            if (second < 0)
            {
                return "Unknown";
            }

            var third = eventId.IndexOf('_', second + 1);
            if (third < 0)
            {
                return eventId.Substring(second + 1);
            }

            return eventId.Substring(second + 1, third - second - 1);
        }

        private void CopyLastPayloadJson()
        {
            if (_records.Count == 0)
            {
                return;
            }

            var last = _records[_records.Count - 1];
            GUIUtility.systemCopyBuffer = string.IsNullOrWhiteSpace(last.PayloadJson)
                ? last.Summary
                : last.PayloadJson;
        }

        private void ExportSnapshot()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var reportDirectory = Path.Combine(projectRoot, ZomCityProjectConstants.Paths.ReportsOutDir.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(reportDirectory);

            var fileName = ZomCityProjectConstants.Reports.FormatJson("GameplayEventViewer", DateTime.UtcNow);
            var reportPath = Path.Combine(reportDirectory, fileName);

            var export = new GameplayEventViewerExport
            {
                GeneratedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Total = _records.Count,
                Records = _records.ToArray(),
            };

            File.WriteAllText(reportPath, JsonUtility.ToJson(export, true) + Environment.NewLine);
            Debug.Log("[ZomCity] EventViewer 导出: " + reportPath);
        }
    }
}
