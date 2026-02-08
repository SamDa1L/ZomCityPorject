using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    /// <summary>
    /// 像素化 RT 管线的“权威视口组件”（唯一口径）：
    /// - 创建/持有低分辨率 World RenderTexture
    /// - 计算 IntegerScale + DisplayRect（Letterbox/Pillarbox）
    /// - 提供 Screen <-> RT <-> World 的统一换算 API
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class PixelViewportManager : MonoBehaviour
    {
        public enum ViewportFitMode
        {
            FillCrop = 0,
            IntegerFit = 1,
        }

        private const string MainCameraPrefabResourcePath = "ZomCity/MainCameraSidescroller";

        public static PixelViewportManager Instance { get; private set; }

        public event Action ViewportChanged;

        [Header("RenderTexture（写死｜按升级流程修改）")]
        [Min(1)] public int RtWidth = 800;
        [Min(1)] public int RtHeight = 450;

        [Header("世界单位 ↔ 像素网格（写死）")]
        [Min(1)] public int PixelsPerUnit = 24;

        [Header("最小窗口护栏（MVP）")]
        [Min(1)] public int MinWindowScale = 2; // 最小窗口 = RT * scale（默认：RT*2）
        public bool EnforceMinWindow = true;

        [Header("视口适配模式（M0.2）")]
        public ViewportFitMode FitMode = ViewportFitMode.FillCrop;

        [Header("玩法平面")]
        public float AimPlaneZ = 0f;


        [Header("镜头缩放（1=默认，2=放大1倍）")]

        [Min(0.01f)] public float CameraZoom = 2f;

        [Header("像素对齐（M0.2 调试可见）")]
        public bool EnablePixelSnapInOrthographicLegacy = true;

        [Header("WorldCamera（可选覆盖）")]
        [Tooltip("若为空：运行时自动创建一台正交 WorldCamera，并由本组件持有其主权。")]
        public Camera WorldCamera;

        [Header("MainCameraSidescroller Follow Defaults")]
        public string DefaultFollowTargetTag = "Player";
        public string DefaultFollowTargetChildName = "";
        [Min(0.05f)] public float DefaultRebindInterval = 0.25f;
        [Min(0.1f)] public float DefaultLostTargetReportDelay = 1f;
        public bool DefaultForceCenterOnBind = true;
        [Min(0.001f)] public float DefaultFollowSmoothness = 0.13f;
        [Range(-1f, 1f)] public float DefaultOffsetX;
        [Range(-1f, 1f)] public float DefaultOffsetY;

        [Header("MainCameraSidescroller State Defaults")]
        public bool DefaultAutoSwitchByInput = true;
        public ZomCityAimStateTriggerMode DefaultAimStateTriggerMode = ZomCityAimStateTriggerMode.DragZoomOnly;
        [Min(1f)] public float DefaultDragObserveRangePx = 480f;
        public ZomCityCameraProfile DefaultCameraProfile;
        public bool ApplyStateProfileOnBoot = true;
        public ZomCityCameraStateDefinition ExploreState = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Explore);
        public ZomCityCameraStateDefinition AimState = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Aim);
        public ZomCityCameraStateDefinition SprintState = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Sprint);
        public ZomCityCameraStateDefinition DeadState = ZomCityCameraStateDefinition.CreatePreset(ZomCityCameraState.Dead);

        public RenderTexture WorldRenderTexture => _worldRt;
        public int IntegerScale => _integerScale;
        public RectInt DisplayRect => _displayRect;
        public float OrthographicSize => (RtHeight / (float)PixelsPerUnit) * 0.5f / Mathf.Max(0.01f, CameraZoom);
        public float RtAspect => RtWidth / (float)RtHeight;
        public bool IsPixelSnapEnabled => EnablePixelSnapInOrthographicLegacy;

        private RenderTexture _worldRt;
        private int _integerScale;
        private RectInt _displayRect;

        private int _lastScreenW;
        private int _lastScreenH;
        private bool _pendingResolutionBounce;

        public static PixelViewportManager EnsureExists()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindAnyObjectByType<PixelViewportManager>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            var prefab = Resources.Load<GameObject>(MainCameraPrefabResourcePath);
            if (prefab != null)
            {
                var goFromPrefab = Instantiate(prefab);
                goFromPrefab.name = "MainCameraSidescroller";

                var mgrFromPrefab = goFromPrefab.GetComponent<PixelViewportManager>();
                if (mgrFromPrefab != null)
                {
                    DontDestroyOnLoad(goFromPrefab);
                    Instance = mgrFromPrefab;
                    return mgrFromPrefab;
                }

                Destroy(goFromPrefab);
            }

            var go = new GameObject("[ZomCity]PixelViewport");
            var mgr = go.AddComponent<PixelViewportManager>();
            DontDestroyOnLoad(go);
            Instance = mgr;
            return mgr;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureWorldCamera();
            EnsureWorldRenderTexture();

            ForceRecalculate();

            // 任何场景加载后，都需要重新禁用“直出屏幕”的相机，避免双重渲染/干扰坐标口径。
            SceneManager.sceneLoaded += OnSceneLoaded;
            DisableOtherSceneCameras();
            EnsureWorldAudioListener();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (Instance == this)
            {
                Instance = null;
            }

            if (_worldRt != null)
            {
                _worldRt.Release();
                Destroy(_worldRt);
                _worldRt = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DisableOtherSceneCameras();
            EnsureWorldAudioListener();
            ForceRecalculate();
        }

        private void Update()
        {
            // 处理窗口缩放/全屏切换等导致的分辨率变化。
            if (_lastScreenW != Screen.width || _lastScreenH != Screen.height)
            {
                ForceRecalculate();
            }
        }

        private void EnsureWorldCamera()
        {
            if (WorldCamera != null)
            {
                ConfigureWorldCamera(WorldCamera);
                return;
            }

            var go = new GameObject("[ZomCity]WorldCamera");
            go.transform.SetParent(transform, worldPositionStays: false);
            WorldCamera = go.AddComponent<Camera>();
            // 默认把相机放在玩法平面后方，确保 nearClip 不会裁掉 Z=0 的玩法对象。
            WorldCamera.transform.localPosition = new Vector3(0f, 0f, -10f);
            WorldCamera.transform.localRotation = Quaternion.identity;
            ConfigureWorldCamera(WorldCamera);
        }

        private void ConfigureWorldCamera(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = OrthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.depth = -100;
            cam.allowHDR = false;
            cam.allowMSAA = false;

            // 兼容临时/第三方代码：仍可通过 Camera.main 找到这台相机。
            cam.tag = "MainCamera";
            EnsureWorldAudioListener();
        }

        private void EnsureWorldAudioListener()
        {
            if (WorldCamera == null)
            {
                return;
            }

            var worldListener = WorldCamera.GetComponent<AudioListener>();
            if (worldListener == null)
            {
                worldListener = WorldCamera.gameObject.AddComponent<AudioListener>();
            }

            if (!worldListener.enabled)
            {
                worldListener.enabled = true;
            }

            var listeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null || listener == worldListener)
                {
                    continue;
                }

                var go = listener.gameObject;
                if (go == null)
                {
                    continue;
                }

                var scene = go.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (listener.enabled)
                {
                    listener.enabled = false;
                }
            }
        }

        private void EnsureWorldRenderTexture()
        {
            if (_worldRt != null && _worldRt.width == RtWidth && _worldRt.height == RtHeight)
            {
                return;
            }

            if (_worldRt != null)
            {
                _worldRt.Release();
                Destroy(_worldRt);
                _worldRt = null;
            }

            var desc = new RenderTextureDescriptor(RtWidth, RtHeight, RenderTextureFormat.ARGB32, depthBufferBits: 24)
            {
                msaaSamples = 1,
                mipCount = 0,
                sRGB = true,
            };

            _worldRt = new RenderTexture(desc)
            {
                name = "ZomCity_WorldRT",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                useMipMap = false,
                autoGenerateMips = false,
            };
            _worldRt.Create();

            if (WorldCamera != null)
            {
                WorldCamera.targetTexture = _worldRt;
                if (WorldCamera.orthographic)
                {
                    WorldCamera.orthographicSize = OrthographicSize;
                }
                WorldCamera.aspect = RtAspect;
            }
        }

        public void ForceRecalculate()
        {
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;

            EnsureWorldRenderTexture();

            if (EnforceMinWindow)
            {
                EnforceMinimumWindowSize();
            }

            RecalculateDisplayRect();
            ViewportChanged?.Invoke();
        }

        private void EnforceMinimumWindowSize()
        {
            // 只在窗口模式下强制最小窗口；全屏相关主权应归 Settings（后续阶段做）。
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                _pendingResolutionBounce = false;
                return;
            }

            var minW = RtWidth * Mathf.Max(1, MinWindowScale);
            var minH = RtHeight * Mathf.Max(1, MinWindowScale);

            if (Screen.width >= minW && Screen.height >= minH)
            {
                _pendingResolutionBounce = false;
                return;
            }

            // 避免每帧狂刷 SetResolution（Unity 是异步生效的）。
            if (_pendingResolutionBounce)
            {
                return;
            }

            _pendingResolutionBounce = true;
            Screen.SetResolution(minW, minH, FullScreenMode.Windowed);
        }

        private void RecalculateDisplayRect()
        {
            var sw = Mathf.Max(1, Screen.width);
            var sh = Mathf.Max(1, Screen.height);

            var scale = ComputeIntegerScale(sw, sh);
            _integerScale = Mathf.Max(1, scale);

            var displayW = RtWidth * _integerScale;
            var displayH = RtHeight * _integerScale;

            // FillCrop 允许出现负偏移，从而确保画面完整铺满屏幕。
            var offsetX = (sw - displayW) / 2;
            var offsetY = (sh - displayH) / 2;
            _displayRect = new RectInt(offsetX, offsetY, displayW, displayH);
        }

        private int ComputeIntegerScale(int screenWidth, int screenHeight)
        {
            var widthRatio = screenWidth / (float)RtWidth;
            var heightRatio = screenHeight / (float)RtHeight;

            if (FitMode == ViewportFitMode.FillCrop)
            {
                // 使用较大比例，保证屏幕被完全覆盖（多余边缘会裁切）。
                return Mathf.CeilToInt(Mathf.Max(widthRatio, heightRatio));
            }

            // 使用较小比例，保证完整画幅（允许上下或左右留边）。
            return Mathf.FloorToInt(Mathf.Min(widthRatio, heightRatio));
        }



        public bool TryScreenToRtPixel(Vector2 screenPixel, out Vector2Int rtPixel)
        {
            rtPixel = default;

            var x = Mathf.FloorToInt(screenPixel.x);
            var y = Mathf.FloorToInt(screenPixel.y);

            if (x < _displayRect.xMin || x >= _displayRect.xMax || y < _displayRect.yMin || y >= _displayRect.yMax)
            {
                return false;
            }

            var localX = x - _displayRect.x;
            var localY = y - _displayRect.y;

            var rx = Mathf.Clamp(localX / _integerScale, 0, RtWidth - 1);
            var ry = Mathf.Clamp(localY / _integerScale, 0, RtHeight - 1);
            rtPixel = new Vector2Int(rx, ry);
            return true;
        }

        public Vector2Int ScreenToRtPixelClamped(Vector2 screenPixel)
        {
            if (TryScreenToRtPixel(screenPixel, out var px))
            {
                return px;
            }

            // 鼠标在 DisplayRect 外：Clamp 到最近边缘（避免崩坏坐标）。
            var x = Mathf.Clamp(Mathf.FloorToInt(screenPixel.x), _displayRect.xMin, _displayRect.xMax - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt(screenPixel.y), _displayRect.yMin, _displayRect.yMax - 1);
            return new Vector2Int(
                Mathf.Clamp((x - _displayRect.x) / _integerScale, 0, RtWidth - 1),
                Mathf.Clamp((y - _displayRect.y) / _integerScale, 0, RtHeight - 1));
        }

        public bool TryScreenToAimPoint(Vector2 screenPixel, out Vector3 aimPoint)
        {
            aimPoint = default;

            if (WorldCamera == null)
            {
                return false;
            }

            if (!TryScreenToRtPixel(screenPixel, out var rtPixel))
            {
                return false;
            }

            return TryRtPixelToAimPoint(rtPixel, out aimPoint);
        }

        public bool TryRtPixelToAimPoint(Vector2Int rtPixel, out Vector3 aimPoint)
        {
            return TryRtPixelToWorld(rtPixel, AimPlaneZ, out aimPoint);
        }

        public bool TryRtPixelToWorld(Vector2Int rtPixel, float planeZ, out Vector3 worldPoint)
        {
            worldPoint = default;

            if (WorldCamera == null)
            {
                return false;
            }

            var u = (rtPixel.x + 0.5f) / RtWidth;
            var v = (rtPixel.y + 0.5f) / RtHeight;

            var ray = WorldCamera.ViewportPointToRay(new Vector3(u, v, 0f));
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }

            worldPoint = ray.GetPoint(enter);
            return true;
        }

        public Vector2 WorldToScreenPixel(Vector3 worldPos)
        {
            if (WorldCamera == null)
            {
                return Vector2.zero;
            }

            var vp = WorldCamera.WorldToViewportPoint(worldPos);
            var rtX = vp.x * RtWidth;
            var rtY = vp.y * RtHeight;

            // 用同一套 DisplayRect/IntegerScale 合同，把 RT 像素换回屏幕像素（UI Overlay 可复用）。
            var sx = _displayRect.x + (rtX * _integerScale);
            var sy = _displayRect.y + (rtY * _integerScale);
            return new Vector2(sx, sy);
        }

        public Vector3 SnapWorldToPixelGrid(Vector3 worldPos)
        {
            if (!EnablePixelSnapInOrthographicLegacy)
            {
                return worldPos;
            }

            var ppu = Mathf.Max(1, PixelsPerUnit);
            var step = 1f / ppu;
            worldPos.x = Mathf.Round(worldPos.x / step) * step;
            worldPos.y = Mathf.Round(worldPos.y / step) * step;
            return worldPos;
        }

        public void ApplyMainCameraDefaults()
        {
            var solver = GetComponent<ZomCityProCameraDriver>();
            if (solver != null)
            {
                solver.FollowTargetTag = string.IsNullOrWhiteSpace(DefaultFollowTargetTag) ? solver.FollowTargetTag : DefaultFollowTargetTag;
                solver.FollowTargetChildName = DefaultFollowTargetChildName;
                solver.RebindInterval = Mathf.Max(0.05f, DefaultRebindInterval);
                solver.LostTargetReportDelay = Mathf.Max(0.1f, DefaultLostTargetReportDelay);
                solver.ForceCenterOnBind = DefaultForceCenterOnBind;
                solver.HorizontalFollowSmoothness = Mathf.Max(0.001f, DefaultFollowSmoothness);
                solver.VerticalFollowSmoothness = Mathf.Max(0.001f, DefaultFollowSmoothness);
                solver.OffsetX = Mathf.Clamp(DefaultOffsetX, -1f, 1f);
                solver.OffsetY = Mathf.Clamp(DefaultOffsetY, -1f, 1f);
                solver.RequestApplyDefaults();
            }

            var stateController = GetComponent<ZomCityCameraStateController>();
            if (stateController == null)
            {
                return;
            }

            stateController.AutoSwitchByInput = DefaultAutoSwitchByInput;
            stateController.AimStateTriggerMode = DefaultAimStateTriggerMode;
            stateController.DragObserveRangePx = Mathf.Max(1f, DefaultDragObserveRangePx);
            if (DefaultCameraProfile != null)
            {
                stateController.ProfileAsset = DefaultCameraProfile;
            }

            if (!ApplyStateProfileOnBoot)
            {
                return;
            }

            if (stateController.TryApplyLocalDefinitions(true))
            {
                return;
            }

            if (stateController.TryApplyProfileAssetDefinitions(true))
            {
                return;
            }

            stateController.OverrideStateDefinitions(
                ExploreState,
                AimState,
                SprintState,
                DeadState,
                true);
        }

        public void DisableOtherSceneCameras()
        {
            if (WorldCamera == null)
            {
                return;
            }

            var cameras = Camera.allCameras;
            for (var i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null || cam == WorldCamera)
                {
                    continue;
                }

                // 只禁用“直出屏幕”的游戏相机：避免误伤 RenderTexture/反射/探针等离屏相机。
                if (!cam.enabled) continue;
                if (cam.targetTexture != null) continue;
                if (cam.cameraType != CameraType.Game) continue;

                cam.enabled = false;
            }
        }
    }
}
