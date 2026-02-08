using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    /// <summary>
    /// 纯代码 Boot 钩子（M0.2）：确保像素化视口链路在每个场景里都存在（无需手动挂载）。
    /// </summary>
    public static class ZomCityRenderingBootRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var viewport = PixelViewportManager.EnsureExists();

            // Presenter 负责把 WorldRT 以整数倍最近邻方式输出到屏幕（UGUI 兜底实现）。
            var go = viewport.gameObject;
            if (go.GetComponent<PixelViewportPresenterUGUI>() == null)
            {
                go.AddComponent<PixelViewportPresenterUGUI>();
            }

            // ProCamera2D Solver：在同一 WorldCamera 主权链路上维持跟随行为。
            if (go.GetComponent<ZomCityProCameraDriver>() == null)
            {
                go.AddComponent<ZomCityProCameraDriver>();
            }

            // Composer 负责最终投影与构图，使 M0.2 可运行 DioramaPerspective。
            if (go.GetComponent<ZomCityCameraComposer>() == null)
            {
                go.AddComponent<ZomCityCameraComposer>();
            }

            // M0.3：按写死 Tick 顺序挂载输入采样与占位 Motor。
            if (go.GetComponent<ZomCityInputSampler>() == null)
            {
                go.AddComponent<ZomCityInputSampler>();
            }

            if (go.GetComponent<ZomCityJUInputBridge>() == null)
            {
                go.AddComponent<ZomCityJUInputBridge>();
            }

            if (go.GetComponent<ZomCityCursorPolicyService>() == null)
            {
                go.AddComponent<ZomCityCursorPolicyService>();
            }

            if (go.GetComponent<ZomCityMotorStub>() == null)
            {
                go.AddComponent<ZomCityMotorStub>();
            }

            if (go.GetComponent<ZomCityCameraStateController>() == null)
            {
                go.AddComponent<ZomCityCameraStateController>();
            }

            if (go.GetComponent<ZomCityAimFireExecutor>() == null)
            {
                go.AddComponent<ZomCityAimFireExecutor>();
            }

            // 运行时调参与调试面板共同满足 M0.2 的双通道调参合同。
            if (go.GetComponent<CameraRuntimeTuningService>() == null)
            {
                go.AddComponent<CameraRuntimeTuningService>();
            }

            if (go.GetComponent<CameraDebugPanel>() == null)
            {
                go.AddComponent<CameraDebugPanel>();
            }

            viewport.ApplyMainCameraDefaults();

            // 抑制旧 JU 相机资产，避免污染 M0.2 的 Time/Camera 基线。
            if (go.GetComponent<ZomCityLegacyCameraSuppressor>() == null)
            {
                go.AddComponent<ZomCityLegacyCameraSuppressor>();
            }
        }
    }

    /// <summary>
    /// M0.2 桥接层：通过 ProCamera2D 驱动 WorldCamera 跟随，
    /// 同时保持 PixelViewportManager 作为渲染/相机主权入口。
    /// </summary>
    [DefaultExecutionOrder(-9980)]
    public sealed class ZomCityProCameraDriver : MonoBehaviour
    {
        private const string ProCameraTypeName = "Com.LuisPedroFonseca.ProCamera2D.ProCamera2D";
        private const string ProCameraAssemblyName = "Assembly-CSharp";

        [Header("运行时引用")]
        public PixelViewportManager Viewport;
        public Transform FollowTarget;

        [Header("跟随目标")]
        public string FollowTargetTag = "Player";
        public string FollowTargetChildName = "";
        [Min(0.05f)] public float RebindInterval = 0.25f;
        [Min(0.1f)] public float LostTargetReportDelay = 1f;
        public bool ForceCenterOnBind = true;

        [Header("ProCamera2D 默认参数（M0.2）")]
        public bool FollowHorizontal = true;
        public bool FollowVertical = true;
        [Min(0.001f)] public float HorizontalFollowSmoothness = 0.12f;
        [Min(0.001f)] public float VerticalFollowSmoothness = 0.14f;
        [Range(-1f, 1f)] public float OffsetX;
        [Range(-1f, 1f)] public float OffsetY;
        public bool IsRelativeOffset = true;
        public bool CenterTargetOnStart = true;
        public bool IgnoreTimeScale;

        private Type _proCameraType;
        private Component _proCamera;
        private Camera _boundWorldCamera;

        private MethodInfo _addCameraTargetMethod;
        private MethodInfo _removeAllCameraTargetsMethod;
        private MethodInfo _centerOnTargetsMethod;

        private Transform _currentFollowTarget;
        private float _nextRebindAt;
        private float _targetMissingSince = -1f;
        private bool _targetLostReported;
        private bool _pluginMissingLogged;

        private bool _defaultsApplied;
        private bool _lastFollowHorizontal;
        private bool _lastFollowVertical;
        private float _lastHorizontalSmoothness;
        private float _lastVerticalSmoothness;
        private float _lastOffsetX;
        private float _lastOffsetY;
        private bool _lastIsRelativeOffset;
        private bool _lastCenterTargetOnStart;
        private bool _lastIgnoreTimeScale;

        public Transform CurrentFollowTarget => _currentFollowTarget;

        private void Awake()
        {
            EnsureViewport();
            EnsureProCameraBinding();
            _nextRebindAt = 0f;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _nextRebindAt = 0f;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _nextRebindAt = 0f;
            _currentFollowTarget = null;
            _defaultsApplied = false;
        }

        private void Update()
        {
            EnsureViewport();
            EnsureProCameraBinding();
            ApplyDefaultSettingsIfNeeded();

            if (Time.unscaledTime < _nextRebindAt)
            {
                return;
            }

            _nextRebindAt = Time.unscaledTime + RebindInterval;
            RefreshFollowTargetBinding();
        }

        public void RequestApplyDefaults()
        {
            _defaultsApplied = false;
        }

        private void EnsureViewport()
        {
            if (Viewport != null)
            {
                return;
            }

            Viewport = PixelViewportManager.Instance != null
                ? PixelViewportManager.Instance
                : PixelViewportManager.EnsureExists();
        }

        private void EnsureProCameraBinding()
        {
            if (Viewport == null || Viewport.WorldCamera == null)
            {
                return;
            }

            if (_boundWorldCamera == Viewport.WorldCamera && _proCamera != null)
            {
                return;
            }

            _proCameraType = ResolveProCameraType();
            if (_proCameraType == null)
            {
                if (!_pluginMissingLogged)
                {
                    _pluginMissingLogged = true;
                    Debug.LogWarning("[ZomCity] ProCamera2D type not found. WorldCamera follow falls back to static camera.");
                }
                return;
            }

            _pluginMissingLogged = false;
            _boundWorldCamera = Viewport.WorldCamera;
            _proCamera = _boundWorldCamera.GetComponent(_proCameraType);
            if (_proCamera == null)
            {
                _proCamera = _boundWorldCamera.gameObject.AddComponent(_proCameraType);
            }

            CacheProCameraApi();
            _defaultsApplied = false;
            ApplyDefaultSettingsIfNeeded();
        }

        private static Type ResolveProCameraType()
        {
            var type = Type.GetType($"{ProCameraTypeName}, {ProCameraAssemblyName}");
            if (type != null)
            {
                return type;
            }

            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetType(ProCameraTypeName, throwOnError: false))
                .FirstOrDefault(t => t != null);
        }

        private void CacheProCameraApi()
        {
            if (_proCamera == null)
            {
                return;
            }

            var t = _proCamera.GetType();
            _addCameraTargetMethod = t.GetMethod(
                "AddCameraTarget",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Transform), typeof(float), typeof(float), typeof(float), typeof(Vector2) },
                null);
            _removeAllCameraTargetsMethod = t.GetMethod(
                "RemoveAllCameraTargets",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(float) },
                null);
            _centerOnTargetsMethod = t.GetMethod("CenterOnTargets", BindingFlags.Instance | BindingFlags.Public);
        }

        private void ApplyDefaultSettingsIfNeeded()
        {
            if (_proCamera == null)
            {
                return;
            }

            if (_defaultsApplied && !DidFollowSettingsChange())
            {
                return;
            }

            ApplyDefaultSettings();
            SnapshotFollowSettings();
            _defaultsApplied = true;
        }

        private void ApplyDefaultSettings()
        {
            if (_proCamera == null)
            {
                return;
            }

            SetEnumProperty(_proCamera, "Axis", "XY");
            SetEnumProperty(_proCamera, "UpdateType", "LateUpdate");

            SetValueProperty(_proCamera, "FollowHorizontal", FollowHorizontal);
            SetValueProperty(_proCamera, "FollowVertical", FollowVertical);
            SetValueProperty(_proCamera, "HorizontalFollowSmoothness", HorizontalFollowSmoothness);
            SetValueProperty(_proCamera, "VerticalFollowSmoothness", VerticalFollowSmoothness);
            SetValueProperty(_proCamera, "OffsetX", OffsetX);
            SetValueProperty(_proCamera, "OffsetY", OffsetY);
            SetValueProperty(_proCamera, "IsRelativeOffset", IsRelativeOffset);
            SetValueProperty(_proCamera, "CenterTargetOnStart", CenterTargetOnStart);
            SetValueProperty(_proCamera, "IgnoreTimeScale", IgnoreTimeScale);
        }

        private bool DidFollowSettingsChange()
        {
            return _lastFollowHorizontal != FollowHorizontal
                   || _lastFollowVertical != FollowVertical
                   || !Mathf.Approximately(_lastHorizontalSmoothness, HorizontalFollowSmoothness)
                   || !Mathf.Approximately(_lastVerticalSmoothness, VerticalFollowSmoothness)
                   || !Mathf.Approximately(_lastOffsetX, OffsetX)
                   || !Mathf.Approximately(_lastOffsetY, OffsetY)
                   || _lastIsRelativeOffset != IsRelativeOffset
                   || _lastCenterTargetOnStart != CenterTargetOnStart
                   || _lastIgnoreTimeScale != IgnoreTimeScale;
        }

        private void SnapshotFollowSettings()
        {
            _lastFollowHorizontal = FollowHorizontal;
            _lastFollowVertical = FollowVertical;
            _lastHorizontalSmoothness = HorizontalFollowSmoothness;
            _lastVerticalSmoothness = VerticalFollowSmoothness;
            _lastOffsetX = OffsetX;
            _lastOffsetY = OffsetY;
            _lastIsRelativeOffset = IsRelativeOffset;
            _lastCenterTargetOnStart = CenterTargetOnStart;
            _lastIgnoreTimeScale = IgnoreTimeScale;
        }

        private static void SetEnumProperty(Component component, string propertyName, string enumValue)
        {
            var t = component.GetType();
            var field = t.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null && field.FieldType.IsEnum)
            {
                TrySetEnum(field.FieldType, enumValue, v => field.SetValue(component, v));
                return;
            }

            var prop = t.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.CanWrite && prop.PropertyType.IsEnum)
            {
                TrySetEnum(prop.PropertyType, enumValue, v => prop.SetValue(component, v));
            }
        }

        private static void TrySetEnum(Type enumType, string enumValue, Action<object> setter)
        {
            try
            {
                var value = Enum.Parse(enumType, enumValue);
                setter(value);
            }
            catch
            {
                // 忽略无效枚举值，避免插件版本差异导致硬失败。
            }
        }

        private static void SetValueProperty(Component component, string propertyName, object value)
        {
            var t = component.GetType();
            var field = t.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                TrySetMember(field.FieldType, value, v => field.SetValue(component, v));
                return;
            }

            var prop = t.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.CanWrite)
            {
                TrySetMember(prop.PropertyType, value, v => prop.SetValue(component, v));
            }
        }

        private static void TrySetMember(Type targetType, object value, Action<object> setter)
        {
            try
            {
                if (value == null)
                {
                    setter(null);
                    return;
                }

                if (targetType.IsAssignableFrom(value.GetType()))
                {
                    setter(value);
                    return;
                }

                setter(Convert.ChangeType(value, targetType));
            }
            catch
            {
                // 忽略不匹配的值类型，保持不同插件版本兼容性。
            }
        }

        private void RefreshFollowTargetBinding()
        {
            if (_proCamera == null || _addCameraTargetMethod == null || _removeAllCameraTargetsMethod == null)
            {
                return;
            }

            var desiredTarget = FollowTarget != null ? FollowTarget : FindPlayerTargetByTag();
            if (desiredTarget == null)
            {
                HandleTargetMissing();
                return;
            }

            if (_currentFollowTarget == desiredTarget)
            {
                _targetMissingSince = -1f;
                _targetLostReported = false;
                return;
            }

            _removeAllCameraTargetsMethod.Invoke(_proCamera, new object[] { 0f });
            _addCameraTargetMethod.Invoke(_proCamera, new object[] { desiredTarget, 1f, 1f, 0f, Vector2.zero });

            if (ForceCenterOnBind && _centerOnTargetsMethod != null)
            {
                _centerOnTargetsMethod.Invoke(_proCamera, null);
            }

            _currentFollowTarget = desiredTarget;
            _targetMissingSince = -1f;
            _targetLostReported = false;
        }

        private Transform FindPlayerTargetByTag()
        {
            if (string.IsNullOrWhiteSpace(FollowTargetTag))
            {
                return null;
            }

            try
            {
                var player = GameObject.FindGameObjectWithTag(FollowTargetTag);
                if (player == null)
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(FollowTargetChildName))
                {
                    var child = player.transform.Find(FollowTargetChildName);
                    if (child != null)
                    {
                        return child;
                    }
                }

                return player.transform;
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private void HandleTargetMissing()
        {
            if (_currentFollowTarget != null && _removeAllCameraTargetsMethod != null)
            {
                _removeAllCameraTargetsMethod.Invoke(_proCamera, new object[] { 0f });
                _currentFollowTarget = null;
            }

            if (_targetLostReported)
            {
                return;
            }

            if (_targetMissingSince < 0f)
            {
                _targetMissingSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _targetMissingSince < LostTargetReportDelay)
            {
                return;
            }

            var evt = new CameraTargetLostEvent
            {
                EventId = GameplayEventIds.CameraTargetLost,
                Frame = Time.frameCount,
                Scene = SceneManager.GetActiveScene().name,
                ActorId = FollowTargetTag,
            };
            GameplayEventHub.Publish(evt);
            _targetLostReported = true;
        }
    }



    public enum ZomCityCursorPolicyState
    {
        Gameplay = 0,
        Aim = 1,
        UI = 2,
        Pause = 3,
    }

    [DefaultExecutionOrder(-11980)]
    [DisallowMultipleComponent]
    public sealed class ZomCityJUInputBridge : MonoBehaviour
    {
        [Header("运行时策略")]
        public bool EnableBridge = true;
        public bool ForceDisableBlockFireModeOnCursorVisible = true;
        [Min(0.05f)] public float PatchInterval = 0.5f;
        public string ActorId = "Player";

        public bool IsFireAllowed => !LastHasBlockingController;
        public bool LastHasBlockingController { get; private set; }
        public string LastFireBlockedReason { get; private set; } = "None";
        public int PatchedControllerCount { get; private set; }

        private const string JuCharacterControllerTypeName = "JUTPS.JUCharacterController";
        private const string JuCharacterControllerAssemblyName = "Assembly-CSharp";
        private const string BlockFireModeFieldName = "BlockFireModeOnCursorVisible";

        private Type _juCharacterControllerType;
        private FieldInfo _blockFireModeField;
        private float _nextPatchAt;

        private void Awake()
        {
            PatchNow();
            RefreshBlockedState(false);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            PatchNow();
            RefreshBlockedState(false);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (!EnableBridge)
            {
                LastHasBlockingController = false;
                LastFireBlockedReason = "BridgeDisabled";
                return;
            }

            if (Time.unscaledTime >= _nextPatchAt)
            {
                PatchNow();
                _nextPatchAt = Time.unscaledTime + Mathf.Max(0.05f, PatchInterval);
            }

            var publishEvent = ZomCityInputSystemCompat.ReadFirePressedThisFrame();
            RefreshBlockedState(publishEvent);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PatchNow();
            RefreshBlockedState(false);
        }

        private void PatchNow()
        {
            PatchedControllerCount = 0;
            if (!EnsureJuControllerType())
            {
                return;
            }

            var controllers = Resources.FindObjectsOfTypeAll(_juCharacterControllerType);
            for (var i = 0; i < controllers.Length; i++)
            {
                if (!(controllers[i] is Component component) || component.gameObject == null)
                {
                    continue;
                }

                var scene = component.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (!ForceDisableBlockFireModeOnCursorVisible)
                {
                    continue;
                }

                var raw = _blockFireModeField.GetValue(component);
                if (!(raw is bool enabled) || !enabled)
                {
                    continue;
                }

                _blockFireModeField.SetValue(component, false);
                PatchedControllerCount++;
            }
        }

        private void RefreshBlockedState(bool publishEvent)
        {
            LastHasBlockingController = HasBlockingController();
            LastFireBlockedReason = LastHasBlockingController ? "JU_BlockFireModeOnCursorVisible" : "None";

            if (!publishEvent || !LastHasBlockingController)
            {
                return;
            }

            GameplayEventHub.Publish(new FireBlockedEvent
            {
                EventId = GameplayEventIds.CombatFireBlocked,
                Frame = Time.frameCount,
                Scene = SceneManager.GetActiveScene().name,
                ActorId = string.IsNullOrEmpty(ActorId) ? "Player" : ActorId,
                Reason = LastFireBlockedReason,
            });
        }

        private bool HasBlockingController()
        {
            if (!EnsureJuControllerType())
            {
                return false;
            }

            var controllers = Resources.FindObjectsOfTypeAll(_juCharacterControllerType);
            for (var i = 0; i < controllers.Length; i++)
            {
                if (!(controllers[i] is Component component) || component.gameObject == null)
                {
                    continue;
                }

                var scene = component.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                var raw = _blockFireModeField.GetValue(component);
                if (!(raw is bool enabled) || !enabled)
                {
                    continue;
                }

                if (Cursor.visible)
                {
                    return true;
                }
            }

            return false;
        }

        private bool EnsureJuControllerType()
        {
            if (_juCharacterControllerType == null)
            {
                _juCharacterControllerType = Type.GetType($"{JuCharacterControllerTypeName}, {JuCharacterControllerAssemblyName}");
                if (_juCharacterControllerType == null)
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    for (var i = 0; i < assemblies.Length; i++)
                    {
                        _juCharacterControllerType = assemblies[i].GetType(JuCharacterControllerTypeName, false);
                        if (_juCharacterControllerType != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (_juCharacterControllerType == null)
            {
                return false;
            }

            if (_blockFireModeField == null || _blockFireModeField.DeclaringType != _juCharacterControllerType)
            {
                _blockFireModeField = _juCharacterControllerType.GetField(BlockFireModeFieldName, BindingFlags.Instance | BindingFlags.Public);
            }

            return _blockFireModeField != null;
        }
    }

    [DefaultExecutionOrder(-11970)]
    [DisallowMultipleComponent]
    public sealed class ZomCityCursorPolicyService : MonoBehaviour
    {
        [Header("运行时引用")]
        public ZomCityInputSampler InputSampler;
        public ZomCityJUInputBridge JUInputBridge;

        [Header("策略开关")]
        public bool EnablePolicy = true;
        public bool AutoStateFromAimInput = true;
        public bool ManualStateOverride;
        public ZomCityCursorPolicyState ManualState = ZomCityCursorPolicyState.Gameplay;

        [Header("Gameplay")]
        public bool GameplayVisible = true;
        public CursorLockMode GameplayLockMode = CursorLockMode.None;

        [Header("Aim")]
        public bool AimVisible = true;
        public CursorLockMode AimLockMode = CursorLockMode.None;

        [Header("UI")]
        public bool UIVisible = true;
        public CursorLockMode UILockMode = CursorLockMode.None;

        [Header("Pause")]
        public bool PauseVisible = true;
        public CursorLockMode PauseLockMode = CursorLockMode.None;

        public ZomCityCursorPolicyState CurrentState { get; private set; } = ZomCityCursorPolicyState.Gameplay;
        public bool CurrentVisible { get; private set; } = true;
        public CursorLockMode CurrentLockMode { get; private set; } = CursorLockMode.None;
        public bool LastCanFire { get; private set; } = true;
        public string LastFireBlockedReason { get; private set; } = "None";

        private void Update()
        {
            EnsureRefs();
            var state = ResolveState();
            ApplyState(state);

            var firePressedThisFrame = ZomCityInputSystemCompat.ReadFirePressedThisFrame();
            RefreshFireState();

            if (firePressedThisFrame && !LastCanFire && !string.Equals(LastFireBlockedReason, "JU_BlockFireModeOnCursorVisible", StringComparison.Ordinal))
            {
                GameplayEventHub.Publish(new FireBlockedEvent
                {
                    EventId = GameplayEventIds.CombatFireBlocked,
                    Frame = Time.frameCount,
                    Scene = SceneManager.GetActiveScene().name,
                    ActorId = InputSampler != null && !string.IsNullOrEmpty(InputSampler.ActorId) ? InputSampler.ActorId : "Player",
                    Reason = LastFireBlockedReason,
                });
            }
        }

        private void EnsureRefs()
        {
            if (InputSampler == null)
            {
                InputSampler = GetComponent<ZomCityInputSampler>();
            }

            if (JUInputBridge == null)
            {
                JUInputBridge = GetComponent<ZomCityJUInputBridge>();
            }
        }

        private ZomCityCursorPolicyState ResolveState()
        {
            if (ManualStateOverride)
            {
                return ManualState;
            }

            if (Time.timeScale <= 0.0001f)
            {
                return ZomCityCursorPolicyState.Pause;
            }

            if (!AutoStateFromAimInput || InputSampler == null)
            {
                return ZomCityCursorPolicyState.Gameplay;
            }

            return InputSampler.CurrentIntent.AimHeld ? ZomCityCursorPolicyState.Aim : ZomCityCursorPolicyState.Gameplay;
        }

        private void ApplyState(ZomCityCursorPolicyState state)
        {
            CurrentState = state;

            if (!EnablePolicy)
            {
                CurrentVisible = Cursor.visible;
                CurrentLockMode = Cursor.lockState;
                return;
            }

            ResolveStateValue(state, out var visible, out var lockMode);

            if (Cursor.visible != visible)
            {
                Cursor.visible = visible;
            }

            if (Cursor.lockState != lockMode)
            {
                Cursor.lockState = lockMode;
            }

            CurrentVisible = Cursor.visible;
            CurrentLockMode = Cursor.lockState;
        }

        private void ResolveStateValue(ZomCityCursorPolicyState state, out bool visible, out CursorLockMode lockMode)
        {
            switch (state)
            {
                case ZomCityCursorPolicyState.Aim:
                    visible = AimVisible;
                    lockMode = AimLockMode;
                    break;
                case ZomCityCursorPolicyState.UI:
                    visible = UIVisible;
                    lockMode = UILockMode;
                    break;
                case ZomCityCursorPolicyState.Pause:
                    visible = PauseVisible;
                    lockMode = PauseLockMode;
                    break;
                default:
                    visible = GameplayVisible;
                    lockMode = GameplayLockMode;
                    break;
            }
        }

        public bool CanFire(out string reason)
        {
            reason = LastFireBlockedReason;
            return LastCanFire;
        }

        private void RefreshFireState()
        {
            if (CurrentState == ZomCityCursorPolicyState.UI)
            {
                LastCanFire = false;
                LastFireBlockedReason = "CursorPolicy_UI";
                return;
            }

            if (CurrentState == ZomCityCursorPolicyState.Pause)
            {
                LastCanFire = false;
                LastFireBlockedReason = "CursorPolicy_Pause";
                return;
            }

            if (JUInputBridge == null)
            {
                LastCanFire = true;
                LastFireBlockedReason = "None";
                return;
            }

            LastCanFire = JUInputBridge.IsFireAllowed;
            LastFireBlockedReason = JUInputBridge.LastFireBlockedReason;
        }
    }



    public enum ZomCityCameraState
    {
        Explore = 0,
        Aim = 1,
        Sprint = 2,
        Dead = 3,
    }

    public enum ZomCityAimStateTriggerMode
    {
        LegacySwitch = 0,
        DragZoomOnly = 1,
        Disabled = 2,
    }

    [Serializable]
    public struct ZomCityPlayerInputIntent
    {
        public int Frame;
        public Vector2 Move;
        public Vector2 AimScreenPosition;
        public bool AimHeld;
        public bool SprintHeld;
        public bool FireHeld;
        public bool FirePressedThisFrame;
        public bool FireRequested;
        public int FireRequestedFrame;
        public float AimDragDeltaX;
        public float AimDragAccumX;
    }

    [Serializable]
    public struct ZomCityPhysicsPose
    {
        public bool IsValid;
        public int FixedFrame;
        public Vector3 PreviousPosition;
        public Vector3 CurrentPosition;

        public Vector3 Sample(float alpha)
        {
            var t = Mathf.Clamp01(alpha);
            return Vector3.Lerp(PreviousPosition, CurrentPosition, t);
        }
    }

    [Serializable]
    public struct ZomCityCameraStateDefinition
    {
        public ZomCityCameraComposer.CameraProjectionMode ProjectionMode;
        public float FieldOfView;
        public float PitchDown;
        public float Yaw;
        public float Distance;
        public float FramingAnchorX;
        public float FramingAnchorY;
        public float Smoothness;
        public bool EnablePixelSnapInOrthographicLegacy;

        public static ZomCityCameraStateDefinition CreatePreset(ZomCityCameraState state)
        {
            switch (state)
            {
                case ZomCityCameraState.Aim:
                    return new ZomCityCameraStateDefinition
                    {
                        ProjectionMode = ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective,
                        FieldOfView = 22f,
                        PitchDown = 9f,
                        Yaw = 0f,
                        Distance = 10f,
                        FramingAnchorX = 0.5f,
                        FramingAnchorY = 0.48f,
                        Smoothness = 0.08f,
                        EnablePixelSnapInOrthographicLegacy = true,
                    };
                case ZomCityCameraState.Sprint:
                    return new ZomCityCameraStateDefinition
                    {
                        ProjectionMode = ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective,
                        FieldOfView = 30f,
                        PitchDown = 12f,
                        Yaw = 0f,
                        Distance = 14f,
                        FramingAnchorX = 0.38f,
                        FramingAnchorY = 0.44f,
                        Smoothness = 0.18f,
                        EnablePixelSnapInOrthographicLegacy = true,
                    };
                case ZomCityCameraState.Dead:
                    return new ZomCityCameraStateDefinition
                    {
                        ProjectionMode = ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective,
                        FieldOfView = 38f,
                        PitchDown = 20f,
                        Yaw = 0f,
                        Distance = 18f,
                        FramingAnchorX = 0.5f,
                        FramingAnchorY = 0.56f,
                        Smoothness = 0.24f,
                        EnablePixelSnapInOrthographicLegacy = false,
                    };
                default:
                    return new ZomCityCameraStateDefinition
                    {
                        ProjectionMode = ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective,
                        FieldOfView = 26f,
                        PitchDown = 10f,
                        Yaw = 0f,
                        Distance = 12f,
                        FramingAnchorX = 0.38f,
                        FramingAnchorY = 0.46f,
                        Smoothness = 0.13f,
                        EnablePixelSnapInOrthographicLegacy = true,
                    };
            }
        }

        public ZomCityCameraStateDefinition Clamp()
        {
            var copy = this;
            copy.FieldOfView = Mathf.Clamp(copy.FieldOfView, 20f, 80f);
            copy.PitchDown = Mathf.Clamp(copy.PitchDown, 0f, 35f);
            copy.Yaw = Mathf.Clamp(copy.Yaw, -10f, 10f);
            copy.Distance = Mathf.Max(0.1f, copy.Distance);
            copy.FramingAnchorX = Mathf.Clamp01(copy.FramingAnchorX);
            copy.FramingAnchorY = Mathf.Clamp01(copy.FramingAnchorY);
            copy.Smoothness = Mathf.Clamp(copy.Smoothness, 0.01f, 0.5f);
            return copy;
        }
    }
    internal static class ZomCityInputSystemCompat
    {
        private const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags StaticPublic = BindingFlags.Static | BindingFlags.Public;

#if ENABLE_INPUT_SYSTEM
        private static Type s_keyboardType;
        private static Type s_mouseType;
        private static PropertyInfo s_keyboardCurrentProperty;
        private static PropertyInfo s_mouseCurrentProperty;
#endif

        public static Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            var right = ReadKeyboardPressed("dKey") || ReadKeyboardPressed("rightArrowKey");
            var left = ReadKeyboardPressed("aKey") || ReadKeyboardPressed("leftArrowKey");
            var up = ReadKeyboardPressed("wKey") || ReadKeyboardPressed("upArrowKey");
            var down = ReadKeyboardPressed("sKey") || ReadKeyboardPressed("downArrowKey");

            var move = new Vector2((right ? 1f : 0f) + (left ? -1f : 0f), (up ? 1f : 0f) + (down ? -1f : 0f));
            return Vector2.ClampMagnitude(move, 1f);
#else
            var x = Input.GetAxisRaw("Horizontal");
            var y = Input.GetAxisRaw("Vertical");
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#endif
        }

        public static bool ReadAimHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return ReadMouseControlBool("rightButton", "isPressed");
#else
            return Input.GetMouseButton(1);
#endif
        }

        public static bool ReadSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return ReadKeyboardPressed("leftShiftKey") || ReadKeyboardPressed("rightShiftKey");
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        public static bool ReadFireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return ReadMouseControlBool("leftButton", "isPressed");
#else
            return Input.GetMouseButton(0);
#endif
        }

        public static bool ReadFirePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return ReadMouseControlBool("leftButton", "wasPressedThisFrame");
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool TryReadPointerScreenPosition(out Vector2 screenPos)
        {
#if ENABLE_INPUT_SYSTEM
            return TryReadMouseVector2("position", out screenPos);
#else
            screenPos = Input.mousePosition;
            return true;
#endif
        }

#if ENABLE_INPUT_SYSTEM
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

        private static bool EnsureMouseType()
        {
            if (s_mouseType != null && s_mouseCurrentProperty != null)
            {
                return true;
            }

            s_mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (s_mouseType == null)
            {
                return false;
            }

            s_mouseCurrentProperty = s_mouseType.GetProperty("current", StaticPublic);
            return s_mouseCurrentProperty != null;
        }

        private static object GetKeyboardInstance()
        {
            if (!EnsureKeyboardType())
            {
                return null;
            }

            return s_keyboardCurrentProperty.GetValue(null, null);
        }

        private static object GetMouseInstance()
        {
            if (!EnsureMouseType())
            {
                return null;
            }

            return s_mouseCurrentProperty.GetValue(null, null);
        }

        private static bool ReadKeyboardPressed(string keyPropertyName)
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
            return ReadControlBool(keyControl, "isPressed");
        }

        private static bool ReadMouseControlBool(string controlName, string boolPropertyName)
        {
            var mouse = GetMouseInstance();
            if (mouse == null)
            {
                return false;
            }

            var controlProperty = mouse.GetType().GetProperty(controlName, InstancePublic);
            if (controlProperty == null)
            {
                return false;
            }

            var control = controlProperty.GetValue(mouse, null);
            return ReadControlBool(control, boolPropertyName);
        }

        private static bool TryReadMouseVector2(string controlName, out Vector2 value)
        {
            value = default;
            var mouse = GetMouseInstance();
            if (mouse == null)
            {
                return false;
            }

            var controlProperty = mouse.GetType().GetProperty(controlName, InstancePublic);
            if (controlProperty == null)
            {
                return false;
            }

            var control = controlProperty.GetValue(mouse, null);
            if (control == null)
            {
                return false;
            }

            var readMethod = control.GetType().GetMethod("ReadValue", Type.EmptyTypes);
            if (readMethod == null)
            {
                return false;
            }

            var raw = readMethod.Invoke(control, null);
            if (raw is Vector2 vec2)
            {
                value = vec2;
                return true;
            }

            return false;
        }

        private static bool ReadControlBool(object control, string boolPropertyName)
        {
            if (control == null)
            {
                return false;
            }

            var property = control.GetType().GetProperty(boolPropertyName, InstancePublic);
            if (property == null)
            {
                return false;
            }

            var raw = property.GetValue(control, null);
            return raw is bool b && b;
        }
#endif
    }

    [DefaultExecutionOrder(-12000)]
    [DisallowMultipleComponent]
    public sealed class ZomCityInputSampler : MonoBehaviour
    {
        [Header("输入采样设置")]
        public bool EnableSampling = true;
        public string ActorId = "Player";

        public ZomCityPlayerInputIntent CurrentIntent { get; private set; }

        private bool _hasPendingFireRequest;
        private int _pendingFireRequestFrame = -1;
        private bool _aimDragWasHeld;
        private float _aimDragLastScreenX;
        private float _aimDragAccumX;

        public bool HasPendingFireRequest => _hasPendingFireRequest;
        public int PendingFireRequestFrame => _pendingFireRequestFrame;

        private void Update()
        {
            var frame = Time.frameCount;

            if (!EnableSampling)
            {
                var emptyIntent = new ZomCityPlayerInputIntent { Frame = frame };
                CurrentIntent = emptyIntent;
                _hasPendingFireRequest = false;
                _pendingFireRequestFrame = -1;
                _aimDragWasHeld = false;
                _aimDragAccumX = 0f;
                _aimDragLastScreenX = 0f;
                return;
            }

            var intent = CurrentIntent;
            intent.Frame = frame;
            intent.Move = ZomCityInputSystemCompat.ReadMove();
            intent.AimHeld = ZomCityInputSystemCompat.ReadAimHeld();
            intent.SprintHeld = ZomCityInputSystemCompat.ReadSprintHeld();
            intent.FireHeld = ZomCityInputSystemCompat.ReadFireHeld();
            intent.FirePressedThisFrame = ZomCityInputSystemCompat.ReadFirePressedThisFrame();

            if (!ZomCityInputSystemCompat.TryReadPointerScreenPosition(out var screenPos))
            {
                screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }

            intent.AimScreenPosition = screenPos;

            var dragDeltaX = 0f;
            if (intent.AimHeld)
            {
                if (!_aimDragWasHeld)
                {
                    _aimDragWasHeld = true;
                    _aimDragLastScreenX = screenPos.x;
                    _aimDragAccumX = 0f;
                }
                else
                {
                    dragDeltaX = screenPos.x - _aimDragLastScreenX;
                    _aimDragAccumX += dragDeltaX;
                    _aimDragLastScreenX = screenPos.x;
                }
            }
            else
            {
                _aimDragWasHeld = false;
                _aimDragLastScreenX = screenPos.x;
                _aimDragAccumX = 0f;
            }

            intent.AimDragDeltaX = dragDeltaX;
            intent.AimDragAccumX = _aimDragAccumX;

            if (intent.FirePressedThisFrame)
            {
                _hasPendingFireRequest = true;
                _pendingFireRequestFrame = frame;

                GameplayEventHub.Publish(new FireRequestedEvent
                {
                    EventId = GameplayEventIds.CombatFireRequested,
                    Frame = frame,
                    Scene = SceneManager.GetActiveScene().name,
                    ActorId = ActorId,
                    ScreenPosition = screenPos,
                });
            }

            intent.FireRequested = _hasPendingFireRequest;
            intent.FireRequestedFrame = _pendingFireRequestFrame;
            CurrentIntent = intent;
        }

        public bool TryConsumeFireRequest(out int requestFrame)
        {
            requestFrame = -1;
            if (!_hasPendingFireRequest)
            {
                return false;
            }

            requestFrame = _pendingFireRequestFrame;
            _hasPendingFireRequest = false;
            _pendingFireRequestFrame = -1;

            var intent = CurrentIntent;
            intent.FireRequested = false;
            CurrentIntent = intent;
            return true;
        }
    }

    [DefaultExecutionOrder(-11900)]
    [DisallowMultipleComponent]
    public sealed class ZomCityMotorStub : MonoBehaviour
    {
        [Header("运行时引用")]
        public PixelViewportManager Viewport;
        public ZomCityProCameraDriver Solver;
        public Transform FallbackTarget;

        public ZomCityPhysicsPose CurrentPose { get; private set; }

        private void FixedUpdate()
        {
            EnsureRefs();

            var target = Solver != null && Solver.CurrentFollowTarget != null
                ? Solver.CurrentFollowTarget
                : FallbackTarget;

            if (target == null)
            {
                CurrentPose = default;
                return;
            }

            var next = target.position;
            if (!CurrentPose.IsValid)
            {
                CurrentPose = new ZomCityPhysicsPose
                {
                    IsValid = true,
                    FixedFrame = Time.frameCount,
                    PreviousPosition = next,
                    CurrentPosition = next,
                };
                return;
            }

            CurrentPose = new ZomCityPhysicsPose
            {
                IsValid = true,
                FixedFrame = Time.frameCount,
                PreviousPosition = CurrentPose.CurrentPosition,
                CurrentPosition = next,
            };
        }

        public Vector3 GetRenderPose(float alpha)
        {
            if (!CurrentPose.IsValid)
            {
                return Vector3.zero;
            }

            return CurrentPose.Sample(alpha);
        }

        private void EnsureRefs()
        {
            if (Viewport == null)
            {
                Viewport = PixelViewportManager.Instance != null
                    ? PixelViewportManager.Instance
                    : PixelViewportManager.EnsureExists();
            }

            if (Solver == null && Viewport != null)
            {
                Solver = Viewport.GetComponent<ZomCityProCameraDriver>();
            }
        }
    }

    [DefaultExecutionOrder(9450)]
    [DisallowMultipleComponent]
    public sealed class ZomCityCameraStateController : MonoBehaviour
    {
        [Serializable]
        private sealed class CameraProfilePersistModel
        {
            public int Version = 2;
            public ZomCityCameraStateDefinition Explore;
            public ZomCityCameraStateDefinition Aim;
            public ZomCityCameraStateDefinition Sprint;
            public ZomCityCameraStateDefinition Dead;
            public ZomCityCameraStateDefinition DragZoomFar;
        }

        [Header("运行时引用")]
        public PixelViewportManager Viewport;
        public ZomCityCameraComposer Composer;
        public CameraRuntimeTuningService Tuning;
        public ZomCityInputSampler InputSampler;

        [Header("状态配置")]
        public ZomCityCameraProfile ProfileAsset;
        public bool AutoSwitchByInput = true;
        public ZomCityAimStateTriggerMode AimStateTriggerMode = ZomCityAimStateTriggerMode.DragZoomOnly;
        [Min(1f)] public float DragObserveRangePx = 480f;

        [Header("瞄准拖拽变焦（M0.3a）")]
        public bool EnableAimDragZoomRuntime = true;
        [Min(1f)] public float DragZoomLeftReserveRangePx = 480f;
        public ZomCityCameraStateDefinition DragZoomFar = CreateDefaultDragZoomFarDefinition();

        public bool ManualStateOverride;
        public ZomCityCameraState ManualState = ZomCityCameraState.Explore;
        public bool ForceDeadState;

        [Header("调试持久化")]
        public bool PersistDebugProfileInDevelopment = true;
        public bool KeepRuntimeDebugValuesBetweenScenes = true;
        public string PersistKey = "ZC_M03_CAMERA_PROFILE_DEBUG_V1";

        public ZomCityCameraState CurrentState => _currentState;
        public float DragNormalized { get; private set; }
        public float ZoomAlpha { get; private set; }

        private ZomCityCameraStateDefinition _explore;
        private ZomCityCameraStateDefinition _aim;
        private ZomCityCameraStateDefinition _sprint;
        private ZomCityCameraStateDefinition _dead;
        private ZomCityCameraState _currentState = ZomCityCameraState.Explore;
        private bool _hasAppliedInitialState;
        private bool _dragZoomRuntimeApplied;

        private void Awake()
        {
            EnsureRefs();
            RestoreDefaultsFromAssetOrPreset();

            if (KeepRuntimeDebugValuesBetweenScenes)
            {
                TryApplyLocalDefinitions(false);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (KeepRuntimeDebugValuesBetweenScenes)
            {
                SaveToLocal();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _hasAppliedInitialState = false;
            _dragZoomRuntimeApplied = false;
        }

        private void Update()
        {
            EnsureRefs();
            UpdateAimDragObserveMetrics();
            if (Composer == null)
            {
                return;
            }

            var desired = ResolveDesiredState();
            if (!_hasAppliedInitialState || desired != _currentState)
            {
                _currentState = desired;
                ApplyStateNow(_currentState, true);
                _hasAppliedInitialState = true;
            }

            var dragApplied = TryApplyRuntimeDragZoom(desired);
            if (_dragZoomRuntimeApplied && !dragApplied)
            {
                ApplyRuntimeDefinition(GetStateDefinition(_currentState), false);
            }

            _dragZoomRuntimeApplied = dragApplied;
        }

        public void ApplyStateNow(ZomCityCameraState state, bool snap)
        {
            _currentState = state;
            ApplyRuntimeDefinition(GetStateDefinition(state), snap);
        }

        private void ApplyRuntimeDefinition(ZomCityCameraStateDefinition definition, bool snap)
        {
            var resolved = definition.Clamp();

            if (Composer != null)
            {
                Composer.ApplyStateDefinition(resolved, snap);
            }

            if (Tuning != null)
            {
                Tuning.SetSmoothness(resolved.Smoothness);
            }

            if (Viewport != null)
            {
                Viewport.EnablePixelSnapInOrthographicLegacy = resolved.EnablePixelSnapInOrthographicLegacy;
            }
        }

        public void CaptureCurrentToState(ZomCityCameraState state)
        {
            EnsureRefs();
            if (Composer == null)
            {
                return;
            }

            var smoothness = Tuning != null ? Tuning.GetSmoothness() : 0.12f;
            var pixelSnap = Viewport != null && Viewport.EnablePixelSnapInOrthographicLegacy;
            var captured = Composer.CaptureCurrentDefinition(smoothness, pixelSnap).Clamp();
            SetStateDefinition(state, captured);
        }

        public void CaptureCurrentToDragZoomFar()
        {
            EnsureRefs();
            if (Composer == null)
            {
                return;
            }

            var smoothness = Tuning != null ? Tuning.GetSmoothness() : 0.12f;
            var pixelSnap = Viewport != null && Viewport.EnablePixelSnapInOrthographicLegacy;
            DragZoomFar = Composer.CaptureCurrentDefinition(smoothness, pixelSnap).Clamp();
        }

        public void RestoreDefaultsFromAssetOrPreset()
        {
            if (TryApplyProfileAssetDefinitions(false))
            {
                return;
            }

            _explore = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Explore);
            _aim = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Aim);
            _sprint = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Sprint);
            _dead = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Dead);
            DragZoomFar = CreateDefaultDragZoomFarDefinition();
        }

        public bool TryApplyProfileAssetDefinitions(bool applyCurrentState)
        {
            if (ProfileAsset == null)
            {
                return false;
            }

            _explore = ProfileAsset.Explore.Clamp();
            _aim = ProfileAsset.Aim.Clamp();
            _sprint = ProfileAsset.Sprint.Clamp();
            _dead = ProfileAsset.Dead.Clamp();
            var dragZoomFar = ProfileAsset.DragZoomFar;
            DragZoomFar = dragZoomFar.Distance > 0.01f ? dragZoomFar.Clamp() : CreateDefaultDragZoomFarDefinition();

            if (!applyCurrentState)
            {
                return true;
            }

            EnsureRefs();
            ApplyStateNow(_currentState, true);
            _hasAppliedInitialState = true;
            return true;
        }

        public bool TryApplyLocalDefinitions(bool applyCurrentState)
        {
            if (!TryLoadFromLocal())
            {
                return false;
            }

            if (!applyCurrentState)
            {
                return true;
            }

            EnsureRefs();
            ApplyStateNow(_currentState, true);
            _hasAppliedInitialState = true;
            return true;
        }

        public void SaveToProfileAsset()
        {
#if UNITY_EDITOR
            if (ProfileAsset == null)
            {
                return;
            }

            ProfileAsset.Explore = _explore;
            ProfileAsset.Aim = _aim;
            ProfileAsset.Sprint = _sprint;
            ProfileAsset.Dead = _dead;
            ProfileAsset.DragZoomFar = DragZoomFar;
            UnityEditor.EditorUtility.SetDirty(ProfileAsset);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public void SaveToLocal()
        {
            if (!PersistDebugProfileInDevelopment || !Debug.isDebugBuild)
            {
                return;
            }
            CaptureCurrentToState(_currentState);

            var data = new CameraProfilePersistModel
            {
                Version = 2,
                Explore = _explore,
                Aim = _aim,
                Sprint = _sprint,
                Dead = _dead,
                DragZoomFar = DragZoomFar,
            };

            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PersistKey, json);
            PlayerPrefs.Save();
        }

        public bool TryLoadFromLocal()
        {
            if (!PersistDebugProfileInDevelopment || !Debug.isDebugBuild)
            {
                return false;
            }

            if (!PlayerPrefs.HasKey(PersistKey))
            {
                return false;
            }

            var json = PlayerPrefs.GetString(PersistKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            var data = JsonUtility.FromJson<CameraProfilePersistModel>(json);
            if (data == null)
            {
                return false;
            }

            _explore = data.Explore.Clamp();
            _aim = data.Aim.Clamp();
            _sprint = data.Sprint.Clamp();
            _dead = data.Dead.Clamp();
            if (data.Version >= 2)
            {
                DragZoomFar = data.DragZoomFar.Clamp();
            }

            return true;
        }

        public void ClearLocalCache()
        {
            if (PlayerPrefs.HasKey(PersistKey))
            {
                PlayerPrefs.DeleteKey(PersistKey);
                PlayerPrefs.Save();
            }
        }

        public void SetManualState(ZomCityCameraState state)
        {
            ManualStateOverride = true;
            ManualState = state;
            ApplyStateNow(state, true);
        }

        public void ExitManualState()
        {
            ManualStateOverride = false;
            _hasAppliedInitialState = false;
        }

        public void OverrideStateDefinitions(
            ZomCityCameraStateDefinition explore,
            ZomCityCameraStateDefinition aim,
            ZomCityCameraStateDefinition sprint,
            ZomCityCameraStateDefinition dead,
            bool applyCurrentState)
        {
            _explore = explore.Clamp();
            _aim = aim.Clamp();
            _sprint = sprint.Clamp();
            _dead = dead.Clamp();

            if (!applyCurrentState)
            {
                return;
            }

            EnsureRefs();
            ApplyStateNow(_currentState, true);
            _hasAppliedInitialState = true;
        }

        public ZomCityCameraStateDefinition GetStateDefinition(ZomCityCameraState state)
        {
            switch (state)
            {
                case ZomCityCameraState.Aim:
                    return _aim;
                case ZomCityCameraState.Sprint:
                    return _sprint;
                case ZomCityCameraState.Dead:
                    return _dead;
                default:
                    return _explore;
            }
        }

        private void SetStateDefinition(ZomCityCameraState state, ZomCityCameraStateDefinition definition)
        {
            switch (state)
            {
                case ZomCityCameraState.Aim:
                    _aim = definition;
                    break;
                case ZomCityCameraState.Sprint:
                    _sprint = definition;
                    break;
                case ZomCityCameraState.Dead:
                    _dead = definition;
                    break;
                default:
                    _explore = definition;
                    break;
            }
        }

        private static ZomCityCameraStateDefinition CreateDefaultDragZoomFarDefinition()
        {
            return new ZomCityCameraStateDefinition
            {
                ProjectionMode = ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective,
                FieldOfView = 34f,
                PitchDown = 10f,
                Yaw = 0f,
                Distance = 18f,
                FramingAnchorX = 0.38f,
                FramingAnchorY = 0.46f,
                Smoothness = 0.13f,
                EnablePixelSnapInOrthographicLegacy = true,
            }.Clamp();
        }

        private void UpdateAimDragObserveMetrics()
        {
            if (InputSampler == null)
            {
                DragNormalized = 0f;
                ZoomAlpha = 0f;
                return;
            }

            var intent = InputSampler.CurrentIntent;
            var rightRange = Mathf.Max(1f, DragObserveRangePx);
            var leftRange = Mathf.Max(1f, DragZoomLeftReserveRangePx);

            var signedNormalized = intent.AimDragAccumX >= 0f
                ? intent.AimDragAccumX / rightRange
                : intent.AimDragAccumX / leftRange;

            DragNormalized = Mathf.Clamp(signedNormalized, -1f, 1f);
            ZoomAlpha = Mathf.Clamp01(DragNormalized);
        }

        private bool TryApplyRuntimeDragZoom(ZomCityCameraState desiredState)
        {
            if (!EnableAimDragZoomRuntime)
            {
                return false;
            }

            if (AimStateTriggerMode != ZomCityAimStateTriggerMode.DragZoomOnly)
            {
                return false;
            }

            if (desiredState != ZomCityCameraState.Explore)
            {
                return false;
            }

            if (InputSampler == null || !InputSampler.CurrentIntent.AimHeld)
            {
                return false;
            }

            var explore = GetStateDefinition(ZomCityCameraState.Explore).Clamp();
            var far = DragZoomFar.Clamp();
            far.ProjectionMode = explore.ProjectionMode;
            var runtime = LerpCameraStateDefinition(explore, far, ZoomAlpha);
            runtime.EnablePixelSnapInOrthographicLegacy = explore.EnablePixelSnapInOrthographicLegacy;
            ApplyRuntimeDefinition(runtime, false);
            return true;
        }

        private static ZomCityCameraStateDefinition LerpCameraStateDefinition(
            ZomCityCameraStateDefinition from,
            ZomCityCameraStateDefinition to,
            float alpha)
        {
            var t = Mathf.Clamp01(alpha);
            return new ZomCityCameraStateDefinition
            {
                ProjectionMode = from.ProjectionMode,
                FieldOfView = Mathf.Lerp(from.FieldOfView, to.FieldOfView, t),
                PitchDown = Mathf.Lerp(from.PitchDown, to.PitchDown, t),
                Yaw = Mathf.Lerp(from.Yaw, to.Yaw, t),
                Distance = Mathf.Lerp(from.Distance, to.Distance, t),
                FramingAnchorX = Mathf.Lerp(from.FramingAnchorX, to.FramingAnchorX, t),
                FramingAnchorY = Mathf.Lerp(from.FramingAnchorY, to.FramingAnchorY, t),
                Smoothness = Mathf.Lerp(from.Smoothness, to.Smoothness, t),
                EnablePixelSnapInOrthographicLegacy = from.EnablePixelSnapInOrthographicLegacy,
            }.Clamp();
        }

        private ZomCityCameraState ResolveDesiredState()
        {
            if (ForceDeadState)
            {
                return ZomCityCameraState.Dead;
            }

            if (ManualStateOverride)
            {
                return ManualState;
            }

            if (!AutoSwitchByInput || InputSampler == null)
            {
                return ZomCityCameraState.Explore;
            }

            var intent = InputSampler.CurrentIntent;
            if (intent.AimHeld)
            {
                switch (AimStateTriggerMode)
                {
                    case ZomCityAimStateTriggerMode.LegacySwitch:
                        return ZomCityCameraState.Aim;
                    case ZomCityAimStateTriggerMode.DragZoomOnly:
                        return ZomCityCameraState.Explore;
                    case ZomCityAimStateTriggerMode.Disabled:
                        break;
                }
            }

            if (intent.SprintHeld)
            {
                return ZomCityCameraState.Sprint;
            }

            return ZomCityCameraState.Explore;
        }

        private void EnsureRefs()
        {
            if (Viewport == null)
            {
                Viewport = PixelViewportManager.Instance != null
                    ? PixelViewportManager.Instance
                    : PixelViewportManager.EnsureExists();
            }

            if (Composer == null && Viewport != null)
            {
                Composer = Viewport.GetComponent<ZomCityCameraComposer>();
            }

            if (Tuning == null && Viewport != null)
            {
                Tuning = Viewport.GetComponent<CameraRuntimeTuningService>();
            }

            if (InputSampler == null && Viewport != null)
            {
                InputSampler = Viewport.GetComponent<ZomCityInputSampler>();
            }
        }
    }

    [DefaultExecutionOrder(9560)]
    [DisallowMultipleComponent]
    public sealed class ZomCityAimFireExecutor : MonoBehaviour
    {
        [Header("运行时引用")]
        public PixelViewportManager Viewport;
        public ZomCityInputSampler InputSampler;
        public ZomCityMotorStub Motor;
        public ZomCityProCameraDriver Solver;

        [Header("调试发射")]
        public LayerMask HitMask = ~0;
        [Min(1f)] public float MaxDistance = 200f;
        public bool DrawDebugLine = true;
        [Min(0f)] public float DebugLineDuration = 0.2f;

        public Vector3 LastAimPoint { get; private set; }
        public int LastConsumedRequestFrame { get; private set; } = -1;
        public bool LastFireHadHit { get; private set; }

        private void LateUpdate()
        {
            EnsureRefs();
            if (Viewport == null || Viewport.WorldCamera == null)
            {
                return;
            }

            var screenPos = InputSampler != null
                ? InputSampler.CurrentIntent.AimScreenPosition
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (!Viewport.TryScreenToAimPoint(screenPos, out var aimPoint))
            {
                var rtPixel = Viewport.ScreenToRtPixelClamped(screenPos);
                if (!Viewport.TryRtPixelToAimPoint(rtPixel, out aimPoint))
                {
                    return;
                }
            }

            LastAimPoint = aimPoint;

            if (InputSampler == null || !InputSampler.TryConsumeFireRequest(out var requestFrame))
            {
                return;
            }

            var cameraPos = Viewport.WorldCamera.transform.position;
            var dir = aimPoint - cameraPos;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Viewport.WorldCamera.transform.forward;
            }

            var ray = new Ray(cameraPos, dir.normalized);
            var hitPoint = aimPoint;
            var hasHit = Physics.Raycast(ray, out var hit, MaxDistance, HitMask, QueryTriggerInteraction.Ignore);
            if (hasHit)
            {
                hitPoint = hit.point;
            }

            LastConsumedRequestFrame = requestFrame;
            LastFireHadHit = hasHit;

            if (DrawDebugLine)
            {
                Debug.DrawLine(cameraPos, hitPoint, hasHit ? Color.green : Color.yellow, DebugLineDuration);
            }

            GameplayEventHub.Publish(new FireConsumedEvent
            {
                EventId = GameplayEventIds.CombatFireConsumed,
                Frame = Time.frameCount,
                Scene = SceneManager.GetActiveScene().name,
                ActorId = Solver != null && Solver.CurrentFollowTarget != null ? Solver.CurrentFollowTarget.name : (InputSampler != null ? InputSampler.ActorId : "Player"),
                RequestFrame = requestFrame,
                ScreenPosition = screenPos,
                AimPoint = aimPoint,
                HasHit = hasHit,
                HitPoint = hitPoint,
            });
        }

        private void EnsureRefs()
        {
            if (Viewport == null)
            {
                Viewport = PixelViewportManager.Instance != null
                    ? PixelViewportManager.Instance
                    : PixelViewportManager.EnsureExists();
            }

            if (InputSampler == null && Viewport != null)
            {
                InputSampler = Viewport.GetComponent<ZomCityInputSampler>();
            }

            if (Motor == null && Viewport != null)
            {
                Motor = Viewport.GetComponent<ZomCityMotorStub>();
            }

            if (Solver == null && Viewport != null)
            {
                Solver = Viewport.GetComponent<ZomCityProCameraDriver>();
            }
        }
    }

    [DefaultExecutionOrder(9500)]
    [DisallowMultipleComponent]
    public sealed class ZomCityCameraComposer : MonoBehaviour
    {
        public enum CameraProjectionMode
        {
            DioramaPerspective = 0,
            OrthographicLegacy = 1,
        }

        [Header("运行时引用")]
        public PixelViewportManager Viewport;
        public ZomCityProCameraDriver Solver;
        public Transform FallbackFollowTarget;

        [Header("投影模式")]
        public CameraProjectionMode ProjectionMode = CameraProjectionMode.DioramaPerspective;

        [Header("Diorama 透视参数")]
        [Range(20f, 80f)] public float FieldOfView = 26f;
        [Range(0f, 35f)] public float PitchDown = 10f;
        [Range(-10f, 10f)] public float Yaw;
        [Min(0.1f)] public float Distance = 12f;
        [Range(0f, 1f)] public float FramingAnchorX = 0.38f;
        [Range(0f, 1f)] public float FramingAnchorY = 0.46f;

        [Header("状态过渡")]
        [Min(0f)] public float BlendDuration = 0.12f;

        [Header("裁剪面")]
        [Min(0.001f)] public float NearClipPlane = 0.03f;
        [Min(1f)] public float FarClipPlane = 500f;

        public Vector3 LastComposedFocusPoint { get; private set; }
        public bool LastPixelSnapApplied { get; private set; }
        public ZomCityCameraStateDefinition LastAppliedDefinition { get; private set; }

        private bool _blendInitialized;
        private ZomCityCameraStateDefinition _smoothedDefinition;

        private void Awake()
        {
            EnsureRefs();
        }

        private void LateUpdate()
        {
            EnsureRefs();

            if (Viewport == null || Viewport.WorldCamera == null)
            {
                return;
            }

            var camera = Viewport.WorldCamera;
            var focus = ResolveFocusPoint(camera, Viewport.AimPlaneZ);
            LastComposedFocusPoint = focus;

            var desired = BuildDesiredDefinition().Clamp();
            var smoothed = SmoothDefinition(desired).Clamp();
            var clamped = smoothed.Clamp();

            if (clamped.ProjectionMode == CameraProjectionMode.DioramaPerspective)
            {
                ApplyDioramaPerspective(camera, focus, clamped);
            }
            else
            {
                ApplyOrthographicLegacy(camera, focus, clamped);
            }

            LastAppliedDefinition = clamped;
        }

        public ZomCityCameraStateDefinition CaptureCurrentDefinition(float smoothness, bool pixelSnap)
        {
            return new ZomCityCameraStateDefinition
            {
                ProjectionMode = ProjectionMode,
                FieldOfView = FieldOfView,
                PitchDown = PitchDown,
                Yaw = Yaw,
                Distance = Distance,
                FramingAnchorX = FramingAnchorX,
                FramingAnchorY = FramingAnchorY,
                Smoothness = smoothness,
                EnablePixelSnapInOrthographicLegacy = pixelSnap,
            }.Clamp();
        }

        public void ApplyStateDefinition(ZomCityCameraStateDefinition definition, bool snap)
        {
            var d = definition.Clamp();
            ProjectionMode = d.ProjectionMode;
            FieldOfView = d.FieldOfView;
            PitchDown = d.PitchDown;
            Yaw = d.Yaw;
            Distance = d.Distance;
            FramingAnchorX = d.FramingAnchorX;
            FramingAnchorY = d.FramingAnchorY;

            if (snap)
            {
                _blendInitialized = false;
            }
        }

        private ZomCityCameraStateDefinition BuildDesiredDefinition()
        {
            return new ZomCityCameraStateDefinition
            {
                ProjectionMode = ProjectionMode,
                FieldOfView = FieldOfView,
                PitchDown = PitchDown,
                Yaw = Yaw,
                Distance = Distance,
                FramingAnchorX = FramingAnchorX,
                FramingAnchorY = FramingAnchorY,
                Smoothness = 0.12f,
                EnablePixelSnapInOrthographicLegacy = Viewport != null && Viewport.IsPixelSnapEnabled,
            };
        }

        private ZomCityCameraStateDefinition SmoothDefinition(ZomCityCameraStateDefinition desired)
        {
            if (!_blendInitialized || _smoothedDefinition.ProjectionMode != desired.ProjectionMode)
            {
                _smoothedDefinition = desired;
                _blendInitialized = true;
                return _smoothedDefinition;
            }

            if (BlendDuration <= 0f)
            {
                _smoothedDefinition = desired;
                return _smoothedDefinition;
            }

            var t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.0001f, BlendDuration));
            _smoothedDefinition.FieldOfView = Mathf.Lerp(_smoothedDefinition.FieldOfView, desired.FieldOfView, t);
            _smoothedDefinition.PitchDown = Mathf.Lerp(_smoothedDefinition.PitchDown, desired.PitchDown, t);
            _smoothedDefinition.Yaw = Mathf.Lerp(_smoothedDefinition.Yaw, desired.Yaw, t);
            _smoothedDefinition.Distance = Mathf.Lerp(_smoothedDefinition.Distance, desired.Distance, t);
            _smoothedDefinition.FramingAnchorX = Mathf.Lerp(_smoothedDefinition.FramingAnchorX, desired.FramingAnchorX, t);
            _smoothedDefinition.FramingAnchorY = Mathf.Lerp(_smoothedDefinition.FramingAnchorY, desired.FramingAnchorY, t);
            _smoothedDefinition.ProjectionMode = desired.ProjectionMode;
            _smoothedDefinition.Smoothness = desired.Smoothness;
            _smoothedDefinition.EnablePixelSnapInOrthographicLegacy = desired.EnablePixelSnapInOrthographicLegacy;
            return _smoothedDefinition;
        }

        private void EnsureRefs()
        {
            if (Viewport == null)
            {
                Viewport = PixelViewportManager.Instance != null
                    ? PixelViewportManager.Instance
                    : PixelViewportManager.EnsureExists();
            }

            if (Solver == null && Viewport != null)
            {
                Solver = Viewport.GetComponent<ZomCityProCameraDriver>();
            }
        }

        private Vector3 ResolveFocusPoint(Camera cam, float planeZ)
        {
            var target = Solver != null && Solver.CurrentFollowTarget != null
                ? Solver.CurrentFollowTarget
                : FallbackFollowTarget;
            if (target != null)
            {
                var pos = target.position;
                pos.z = planeZ;
                return pos;
            }

            if (TryGetPlaneIntersection(cam, new Vector2(0.5f, 0.5f), planeZ, out var focus))
            {
                return focus;
            }

            var fallback = cam.transform.position;
            fallback.z = planeZ;
            return fallback;
        }

        private void ApplyDioramaPerspective(Camera cam, Vector3 focus, ZomCityCameraStateDefinition definition)
        {
            var rotation = Quaternion.Euler(definition.PitchDown, definition.Yaw, 0f);
            var anchor = new Vector2(definition.FramingAnchorX, definition.FramingAnchorY);

            cam.orthographic = false;
            cam.fieldOfView = definition.FieldOfView;
            cam.aspect = Viewport != null ? Viewport.RtAspect : cam.aspect;
            cam.nearClipPlane = Mathf.Max(0.001f, NearClipPlane);
            cam.farClipPlane = Mathf.Max(cam.nearClipPlane + 1f, FarClipPlane);

            var rayDir = BuildAnchorRayDirection(rotation, cam.fieldOfView, cam.aspect, anchor);
            var forward = rotation * Vector3.forward;

            var denom = Mathf.Max(0.0001f, Vector3.Dot(rayDir, forward));
            var rayDistance = Mathf.Max(0.1f, definition.Distance) / denom;
            var cameraPos = focus - rayDir * rayDistance;

            LastPixelSnapApplied = false;
            cam.transform.SetPositionAndRotation(cameraPos, rotation);
        }

        private void ApplyOrthographicLegacy(Camera cam, Vector3 focus, ZomCityCameraStateDefinition definition)
        {
            cam.orthographic = true;
            cam.orthographicSize = Viewport != null ? Viewport.OrthographicSize : cam.orthographicSize;
            cam.nearClipPlane = Mathf.Max(0.001f, NearClipPlane);
            cam.farClipPlane = Mathf.Max(cam.nearClipPlane + 1f, FarClipPlane);

            var depth = Mathf.Max(0.1f, definition.Distance);
            var cameraPos = new Vector3(focus.x, focus.y, focus.z - depth);

            var canSnap = definition.EnablePixelSnapInOrthographicLegacy && Viewport != null && Viewport.IsPixelSnapEnabled;
            if (canSnap)
            {
                cameraPos = Viewport.SnapWorldToPixelGrid(cameraPos);
            }

            LastPixelSnapApplied = canSnap;
            cam.transform.SetPositionAndRotation(cameraPos, Quaternion.identity);
        }

        private static Vector3 BuildAnchorRayDirection(Quaternion rotation, float fov, float aspect, Vector2 anchor)
        {
            var tanHalfFov = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
            var nx = (anchor.x - 0.5f) * 2f;
            var ny = (anchor.y - 0.5f) * 2f;

            var localDir = new Vector3(nx * tanHalfFov * aspect, ny * tanHalfFov, 1f).normalized;
            return rotation * localDir;
        }

        private static bool TryGetPlaneIntersection(Camera cam, Vector2 viewportPoint, float planeZ, out Vector3 hitPoint)
        {
            hitPoint = default;
            if (cam == null)
            {
                return false;
            }

            var ray = cam.ViewportPointToRay(new Vector3(viewportPoint.x, viewportPoint.y, 0f));
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }

            hitPoint = ray.GetPoint(enter);
            return true;
        }
    }


    [DefaultExecutionOrder(9550)]
    [DisallowMultipleComponent]
    public sealed class CameraRuntimeTuningService : MonoBehaviour
    {
        [Header("运行时引用")]
        public PixelViewportManager Viewport;
        public ZomCityCameraComposer Composer;
        public ZomCityProCameraDriver Solver;

        private void Awake()
        {
            EnsureRefs();
        }

        private void Update()
        {
            EnsureRefs();

            if (Solver != null)
            {
                Solver.RequestApplyDefaults();
            }
        }

        public bool IsReady => Viewport != null && Composer != null;

        public float GetSmoothness()
        {
            if (Solver == null)
            {
                return 0f;
            }

            return (Solver.HorizontalFollowSmoothness + Solver.VerticalFollowSmoothness) * 0.5f;
        }

        public void SetSmoothness(float value)
        {
            if (Solver == null)
            {
                return;
            }

            var v = Mathf.Max(0.001f, value);
            Solver.HorizontalFollowSmoothness = v;
            Solver.VerticalFollowSmoothness = v;
            Solver.RequestApplyDefaults();
        }

        public void SetProjectionMode(ZomCityCameraComposer.CameraProjectionMode mode)
        {
            if (Composer == null)
            {
                return;
            }

            Composer.ProjectionMode = mode;
        }

        public void SetFieldOfView(float value)
        {
            if (Composer == null)
            {
                return;
            }

            Composer.FieldOfView = Mathf.Clamp(value, 20f, 80f);
        }

        public void SetPitch(float value)
        {
            if (Composer == null)
            {
                return;
            }

            Composer.PitchDown = Mathf.Clamp(value, 0f, 35f);
        }

        public void SetYaw(float value)
        {
            if (Composer == null)
            {
                return;
            }

            Composer.Yaw = Mathf.Clamp(value, -10f, 10f);
        }

        public void SetDistance(float value)
        {
            if (Composer == null)
            {
                return;
            }

            Composer.Distance = Mathf.Max(0.1f, value);
        }

        public void SetFramingAnchor(float x, float y)
        {
            if (Composer == null)
            {
                return;
            }

            Composer.FramingAnchorX = Mathf.Clamp01(x);
            Composer.FramingAnchorY = Mathf.Clamp01(y);
        }

        private void EnsureRefs()
        {
            if (Viewport == null)
            {
                Viewport = PixelViewportManager.Instance != null
                    ? PixelViewportManager.Instance
                    : PixelViewportManager.EnsureExists();
            }

            if (Composer == null && Viewport != null)
            {
                Composer = Viewport.GetComponent<ZomCityCameraComposer>();
            }

            if (Solver == null && Viewport != null)
            {
                Solver = Viewport.GetComponent<ZomCityProCameraDriver>();
            }
        }
    }


    [DefaultExecutionOrder(9600)]
    [DisallowMultipleComponent]
    public sealed class CameraDebugPanel : MonoBehaviour
    {
        public bool Visible = true;
        public KeyCode ToggleKey = KeyCode.F1;

        [Header("运行时引用")]
        public PixelViewportManager Viewport;
        public ZomCityCameraComposer Composer;
        public CameraRuntimeTuningService Tuning;
        public ZomCityCameraStateController StateController;
        public ZomCityInputSampler InputSampler;
        public ZomCityAimFireExecutor AimFireExecutor;
        public ZomCityJUInputBridge JUInputBridge;
        public ZomCityCursorPolicyService CursorPolicyService;

        private Rect _windowRect = new Rect(12f, 12f, 520f, 720f);
        private string _dragFarFovInput = "34.000";
        private string _dragFarPitchInput = "10.000";
        private string _dragFarYawInput = "0.000";
        private string _dragFarDistanceInput = "18.000";
        private string _dragFarAnchorXInput = "0.380";
        private string _dragFarAnchorYInput = "0.460";
        private string _dragFarSmoothnessInput = "0.130";
        private bool _dragFarInputsInitialized;

        private void Awake()
        {
            EnsureRefs();
        }

        private void Update()
        {
            if (WasTogglePressed())
            {
                Visible = !Visible;
            }

            EnsureRefs();
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return WasTogglePressedWithInputSystem();
#else
            return Input.GetKeyDown(ToggleKey);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static Type s_inputSystemKeyboardType;
        private static PropertyInfo s_keyboardCurrentProperty;
        private static PropertyInfo s_wasPressedProperty;

        private bool WasTogglePressedWithInputSystem()
        {
            if (!EnsureInputSystemKeyboardType())
            {
                return false;
            }

            var keyboard = s_keyboardCurrentProperty.GetValue(null, null);
            if (keyboard == null)
            {
                return false;
            }

            if (!TryMapToggleKeyPropertyName(out var propertyName))
            {
                propertyName = "f1Key";
            }

            var keyProperty = s_inputSystemKeyboardType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (keyProperty == null)
            {
                return false;
            }

            var keyControl = keyProperty.GetValue(keyboard, null);
            if (keyControl == null)
            {
                return false;
            }

            if (s_wasPressedProperty == null || s_wasPressedProperty.DeclaringType != keyControl.GetType())
            {
                s_wasPressedProperty = keyControl.GetType().GetProperty("wasPressedThisFrame", BindingFlags.Instance | BindingFlags.Public);
            }

            if (s_wasPressedProperty == null)
            {
                return false;
            }

            var pressedValue = s_wasPressedProperty.GetValue(keyControl, null);
            return pressedValue is bool pressed && pressed;
        }

        private static bool EnsureInputSystemKeyboardType()
        {
            if (s_inputSystemKeyboardType != null && s_keyboardCurrentProperty != null)
            {
                return true;
            }

            s_inputSystemKeyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            if (s_inputSystemKeyboardType == null)
            {
                return false;
            }

            s_keyboardCurrentProperty = s_inputSystemKeyboardType.GetProperty("current", BindingFlags.Static | BindingFlags.Public);
            return s_keyboardCurrentProperty != null;
        }

        private bool TryMapToggleKeyPropertyName(out string propertyName)
        {
            switch (ToggleKey)
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
                default:
                    propertyName = null;
                    return false;
            }
        }
#endif

        private void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "[ZomCity] Camera Debug");
        }

        private void DrawWindow(int id)
        {
            EnsureRefs();

            GUILayout.BeginVertical();

            if (Viewport == null || Viewport.WorldCamera == null)
            {
                GUILayout.Label("Viewport/WorldCamera 缺失");
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }

            var cam = Viewport.WorldCamera;
            var rect = Viewport.DisplayRect;

            GUILayout.Label($"RT: {Viewport.RtWidth}x{Viewport.RtHeight}  FitMode={Viewport.FitMode}  Scale={Viewport.IntegerScale}");
            GUILayout.Label($"DisplayRect: x={rect.x} y={rect.y} w={rect.width} h={rect.height}");

            if (Composer == null)
            {
                GUILayout.Label("Composer 缺失");
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }

            GUILayout.Space(6f);
            GUILayout.Label($"Mode: {Composer.ProjectionMode}");

            if (StateController != null)
            {
                GUILayout.Label($"CameraState={StateController.CurrentState}  Manual={StateController.ManualStateOverride}  Auto={StateController.AutoSwitchByInput}");
                GUILayout.Label($"AimTriggerMode={StateController.AimStateTriggerMode}  DragNorm={StateController.DragNormalized:F3}  ZoomAlpha={StateController.ZoomAlpha:F3}");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("手动Explore")) StateController.SetManualState(ZomCityCameraState.Explore);
                if (GUILayout.Button("手动Aim")) StateController.SetManualState(ZomCityCameraState.Aim);
                if (GUILayout.Button("手动Sprint")) StateController.SetManualState(ZomCityCameraState.Sprint);
                if (GUILayout.Button("手动Dead")) StateController.SetManualState(ZomCityCameraState.Dead);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("退出手动状态")) StateController.ExitManualState();
                if (GUILayout.Button("写入当前状态参数（仅本次运行）")) StateController.CaptureCurrentToState(StateController.CurrentState);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("恢复默认Profile"))
                {
                    StateController.RestoreDefaultsFromAssetOrPreset();
                    StateController.ApplyStateNow(StateController.CurrentState, true);
                    SyncDragZoomFarInputFields(StateController.DragZoomFar);
                }

                if (GUILayout.Button("保存到Profile资产"))
                {
                    StateController.SaveToProfileAsset();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("保存本地调试参数（跨重启）")) StateController.SaveToLocal();
                if (GUILayout.Button("读取本地调试参数"))
                {
                    StateController.TryApplyLocalDefinitions(true);
                    SyncDragZoomFarInputFields(StateController.DragZoomFar);
                }
                if (GUILayout.Button("清理本地缓存")) StateController.ClearLocalCache();
                GUILayout.EndHorizontal();
                GUILayout.Label("保存本地调试参数会自动写入当前状态并跨重启保留。", GUILayout.Height(32f));

                StateController.AutoSwitchByInput = GUILayout.Toggle(StateController.AutoSwitchByInput, "启用输入驱动状态切换");
                StateController.EnableAimDragZoomRuntime = GUILayout.Toggle(StateController.EnableAimDragZoomRuntime, "启用瞄准拖拽变焦（M0.3a）");
                DrawSlider("DragOutRangePx", 120f, 2400f, StateController.DragObserveRangePx, v => StateController.DragObserveRangePx = v);
                DrawSlider("DragLeftReservePx", 120f, 2400f, StateController.DragZoomLeftReserveRangePx, v => StateController.DragZoomLeftReserveRangePx = v);

                if (GUILayout.Button("写入当前为拉远上限"))
                {
                    StateController.CaptureCurrentToDragZoomFar();
                    SyncDragZoomFarInputFields(StateController.DragZoomFar);
                }

                DrawDragZoomFarEditor();

                if (GUILayout.Button("切换瞄准触发模式"))
                {
                    var next = (int)StateController.AimStateTriggerMode + 1;
                    if (next > (int)ZomCityAimStateTriggerMode.Disabled)
                    {
                        next = 0;
                    }

                    StateController.AimStateTriggerMode = (ZomCityAimStateTriggerMode)next;
                }

                StateController.PersistDebugProfileInDevelopment = GUILayout.Toggle(StateController.PersistDebugProfileInDevelopment, "Development 持久化调试参数");
                StateController.KeepRuntimeDebugValuesBetweenScenes = GUILayout.Toggle(StateController.KeepRuntimeDebugValuesBetweenScenes, "切场景保留运行时参数");
            }

            if (GUILayout.Button("切换投影模式 (Diorama/Ortho)"))
            {
                Composer.ProjectionMode = Composer.ProjectionMode == ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective
                    ? ZomCityCameraComposer.CameraProjectionMode.OrthographicLegacy
                    : ZomCityCameraComposer.CameraProjectionMode.DioramaPerspective;
            }

            GUILayout.Space(4f);
            DrawSlider("FOV", 20f, 80f, Composer.FieldOfView, v => Composer.FieldOfView = v);
            DrawSlider("PitchDown", 0f, 35f, Composer.PitchDown, v => Composer.PitchDown = v);
            DrawSlider("Yaw", -10f, 10f, Composer.Yaw, v => Composer.Yaw = v);
            DrawSlider("Distance", 1f, 40f, Composer.Distance, v => Composer.Distance = v);
            DrawSlider("FramingAnchorX", 0f, 1f, Composer.FramingAnchorX, v => Composer.FramingAnchorX = v);
            DrawSlider("FramingAnchorY", 0f, 1f, Composer.FramingAnchorY, v => Composer.FramingAnchorY = v);

            if (Tuning != null)
            {
                var smooth = Tuning.GetSmoothness();
                DrawSlider("Smoothness", 0.01f, 0.5f, smooth, Tuning.SetSmoothness);
            }

            GUILayout.Space(6f);
            GUILayout.Label($"Camera.orthographic={cam.orthographic}  aspect={cam.aspect:F3}");
            GUILayout.Label($"PPU={Viewport.PixelsPerUnit}  CameraZoom={Viewport.CameraZoom:F2}  OrthoSize={Viewport.OrthographicSize:F3}");
            GUILayout.Label($"AimPlaneZ={Viewport.AimPlaneZ:F2}  FitMode={Viewport.FitMode}  IntegerScale={Viewport.IntegerScale}");

            var pixelSnapStatus = "Disabled";
            if (Viewport.IsPixelSnapEnabled)
            {
                pixelSnapStatus = Composer.LastPixelSnapApplied ? "EnabledApplied" : "EnabledBypass";
            }

            GUILayout.Label($"PixelSnap={pixelSnapStatus}");

            var roundTripError = MeasureAimRoundTripErrorPx();
            GUILayout.Label($"AimRoundTripMaxErrPx={roundTripError:F3}");

            if (InputSampler != null)
            {
                var intent = InputSampler.CurrentIntent;
                GUILayout.Label($"InputFrame={intent.Frame} FireHeld={intent.FireHeld} FireRequested={InputSampler.HasPendingFireRequest} ReqFrame={InputSampler.PendingFireRequestFrame}");
                GUILayout.Label($"Move=({intent.Move.x:F2},{intent.Move.y:F2}) AimScreen=({intent.AimScreenPosition.x:F1},{intent.AimScreenPosition.y:F1})");
                GUILayout.Label($"AimDragDeltaX={intent.AimDragDeltaX:F2}  AimDragAccumX={intent.AimDragAccumX:F2}");
            }

            if (JUInputBridge != null)
            {
                GUILayout.Label($"JUBridge FireAllowed={JUInputBridge.IsFireAllowed} BlockReason={JUInputBridge.LastFireBlockedReason} Patched={JUInputBridge.PatchedControllerCount}");
            }

            if (CursorPolicyService != null)
            {
                GUILayout.Label($"CursorPolicy={CursorPolicyService.CurrentState} Visible={CursorPolicyService.CurrentVisible} Lock={CursorPolicyService.CurrentLockMode}");
                GUILayout.Label($"CanFire={CursorPolicyService.LastCanFire} FireBlockedReason={CursorPolicyService.LastFireBlockedReason}");
            }

            if (AimFireExecutor != null)
            {
                GUILayout.Label($"LateFireConsumedFrame={AimFireExecutor.LastConsumedRequestFrame} Hit={AimFireExecutor.LastFireHadHit}");
                GUILayout.Label($"LateAimPoint=({AimFireExecutor.LastAimPoint.x:F2},{AimFireExecutor.LastAimPoint.y:F2},{AimFireExecutor.LastAimPoint.z:F2})");
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawDragZoomFarEditor()
        {
            if (StateController == null)
            {
                return;
            }

            var dragFar = StateController.DragZoomFar.Clamp();
            if (!_dragFarInputsInitialized)
            {
                SyncDragZoomFarInputFields(dragFar);
            }

            GUILayout.Space(4f);
            GUILayout.Label($"DragFar FOV={dragFar.FieldOfView:F1} Dist={dragFar.Distance:F1} Smooth={dragFar.Smoothness:F2}");
            DrawSliderWithInput("DragFar.FOV", 20f, 80f, ref _dragFarFovInput, dragFar.FieldOfView, v => dragFar.FieldOfView = v);
            DrawSliderWithInput("DragFar.Pitch", 0f, 35f, ref _dragFarPitchInput, dragFar.PitchDown, v => dragFar.PitchDown = v);
            DrawSliderWithInput("DragFar.Yaw", -10f, 10f, ref _dragFarYawInput, dragFar.Yaw, v => dragFar.Yaw = v);
            DrawSliderWithInput("DragFar.Distance", 0.1f, 40f, ref _dragFarDistanceInput, dragFar.Distance, v => dragFar.Distance = v);
            DrawSliderWithInput("DragFar.AnchorX", 0f, 1f, ref _dragFarAnchorXInput, dragFar.FramingAnchorX, v => dragFar.FramingAnchorX = v);
            DrawSliderWithInput("DragFar.AnchorY", 0f, 1f, ref _dragFarAnchorYInput, dragFar.FramingAnchorY, v => dragFar.FramingAnchorY = v);
            DrawSliderWithInput("DragFar.Smooth", 0.01f, 0.5f, ref _dragFarSmoothnessInput, dragFar.Smoothness, v => dragFar.Smoothness = v);

            dragFar.EnablePixelSnapInOrthographicLegacy = GUILayout.Toggle(
                dragFar.EnablePixelSnapInOrthographicLegacy,
                "DragFar PixelSnap (OrthoLegacy)");

            if (GUILayout.Button("Sync DragFar Inputs"))
            {
                SyncDragZoomFarInputFields(dragFar);
            }

            StateController.DragZoomFar = dragFar.Clamp();
        }

        private void SyncDragZoomFarInputFields(ZomCityCameraStateDefinition definition)
        {
            var d = definition.Clamp();
            _dragFarFovInput = d.FieldOfView.ToString("F3");
            _dragFarPitchInput = d.PitchDown.ToString("F3");
            _dragFarYawInput = d.Yaw.ToString("F3");
            _dragFarDistanceInput = d.Distance.ToString("F3");
            _dragFarAnchorXInput = d.FramingAnchorX.ToString("F3");
            _dragFarAnchorYInput = d.FramingAnchorY.ToString("F3");
            _dragFarSmoothnessInput = d.Smoothness.ToString("F3");
            _dragFarInputsInitialized = true;
        }

        private static void DrawSliderWithInput(string label, float min, float max, ref string input, float value, System.Action<float> setter)
        {
            GUILayout.Label($"{label}: {value:F3}");
            var next = GUILayout.HorizontalSlider(value, min, max);
            if (!Mathf.Approximately(next, value))
            {
                setter(next);
                input = next.ToString("F3");
            }

            GUILayout.BeginHorizontal();
            input = GUILayout.TextField(input, GUILayout.Width(96f));
            if (GUILayout.Button("Apply", GUILayout.Width(52f)))
            {
                if (TryParseFloatInput(input, out var parsed))
                {
                    var clamped = Mathf.Clamp(parsed, min, max);
                    setter(clamped);
                    input = clamped.ToString("F3");
                }
            }
            GUILayout.EndHorizontal();
        }

        private static bool TryParseFloatInput(string text, out float value)
        {
            if (float.TryParse(text, out value))
            {
                return true;
            }

            var normalized = string.IsNullOrEmpty(text) ? string.Empty : text.Replace(",", ".");
            return float.TryParse(
                normalized,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        private float MeasureAimRoundTripErrorPx()
        {
            if (Viewport == null)
            {
                return -1f;
            }

            var rect = Viewport.DisplayRect;
            if (rect.width <= 0 || rect.height <= 0)
            {
                return -1f;
            }

            var samples = new[]
            {
                new Vector2(rect.x + 1f, rect.y + 1f),
                new Vector2(rect.xMax - 2f, rect.y + 1f),
                new Vector2(rect.x + 1f, rect.yMax - 2f),
                new Vector2(rect.xMax - 2f, rect.yMax - 2f),
                new Vector2(rect.center.x, rect.center.y),
            };

            var maxErr = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                if (!Viewport.TryScreenToAimPoint(samples[i], out var aimPoint))
                {
                    continue;
                }

                var screenBack = Viewport.WorldToScreenPixel(aimPoint);
                var err = Vector2.Distance(samples[i], screenBack);
                if (err > maxErr)
                {
                    maxErr = err;
                }
            }

            return maxErr;
        }

        private static void DrawSlider(string label, float min, float max, float value, System.Action<float> setter)
        {
            GUILayout.Label($"{label}: {value:F3}");
            var next = GUILayout.HorizontalSlider(value, min, max);
            if (!Mathf.Approximately(next, value))
            {
                setter(next);
            }
        }

        private void EnsureRefs()
        {
            if (Viewport == null)
            {
                Viewport = PixelViewportManager.Instance != null
                    ? PixelViewportManager.Instance
                    : PixelViewportManager.EnsureExists();
            }

            if (Composer == null && Viewport != null)
            {
                Composer = Viewport.GetComponent<ZomCityCameraComposer>();
            }

            if (Tuning == null && Viewport != null)
            {
                Tuning = Viewport.GetComponent<CameraRuntimeTuningService>();
            }

            if (StateController == null && Viewport != null)
            {
                StateController = Viewport.GetComponent<ZomCityCameraStateController>();
            }

            if (InputSampler == null && Viewport != null)
            {
                InputSampler = Viewport.GetComponent<ZomCityInputSampler>();
            }

            if (AimFireExecutor == null && Viewport != null)
            {
                AimFireExecutor = Viewport.GetComponent<ZomCityAimFireExecutor>();
            }

            if (JUInputBridge == null && Viewport != null)
            {
                JUInputBridge = Viewport.GetComponent<ZomCityJUInputBridge>();
            }

            if (CursorPolicyService == null && Viewport != null)
            {
                CursorPolicyService = Viewport.GetComponent<ZomCityCursorPolicyService>();
            }
        }
    }

    [DefaultExecutionOrder(-9975)]
    public sealed class ZomCityLegacyCameraSuppressor : MonoBehaviour
    {
        private static readonly string[] LegacyComponentTypeNames =
        {
            "JUTPS.CameraSystems.JUCameraSystemLib",
            "JUTPS.CameraSystems.TPSCameraController",
            "JUTPS.CameraSystems.SidescrollerCameraController",
        };

        private void Awake()
        {
            SuppressLegacyCameras();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SuppressLegacyCameras();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SuppressLegacyCameras();
        }

        private static void SuppressLegacyCameras()
        {
            var viewport = PixelViewportManager.Instance;
            var keep = viewport != null ? viewport.WorldCamera : null;

            var cameras = Camera.allCameras;
            for (var i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null || cam == keep)
                {
                    continue;
                }

                var node = FindLegacyCameraNode(cam.transform);
                if (node == null)
                {
                    continue;
                }

                if (node.activeSelf)
                {
                    node.SetActive(false);
                }
            }
        }

        private static GameObject FindLegacyCameraNode(Transform start)
        {
            var cursor = start;
            while (cursor != null)
            {
                if (IsLegacyCameraNode(cursor.gameObject))
                {
                    return cursor.gameObject;
                }

                cursor = cursor.parent;
            }

            return null;
        }

        private static bool IsLegacyCameraNode(GameObject go)
        {
            if (go.name.IndexOf("Sidescroller Camera Controller", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var behaviours = go.GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b == null)
                {
                    continue;
                }

                var fullName = b.GetType().FullName;
                if (string.IsNullOrEmpty(fullName))
                {
                    continue;
                }

                for (var j = 0; j < LegacyComponentTypeNames.Length; j++)
                {
                    if (string.Equals(fullName, LegacyComponentTypeNames[j], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}



