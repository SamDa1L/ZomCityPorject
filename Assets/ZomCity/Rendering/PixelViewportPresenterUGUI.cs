using UnityEngine;
using UnityEngine.UI;

namespace ZomCity
{
    /// <summary>
    /// 使用 UGUI 将 World RenderTexture 输出到屏幕，保持整数倍缩放与最近邻采样。
    /// 注意：UI Overlay 必须使用原生分辨率渲染，避免文字和图标变糊。
    /// </summary>
    [DefaultExecutionOrder(-9990)]
    public sealed class PixelViewportPresenterUGUI : MonoBehaviour
    {
        private const string DisplayBridgeCameraName = "[ZomCity]DisplayBridgeCamera";

        [Tooltip("若为空则使用 PixelViewportManager.Instance。")]
        public PixelViewportManager Viewport;

        [Header("可选覆盖")]
        public Canvas OutputCanvas;
        public RawImage OutputImage;
        public Camera DisplayBridgeCamera;

        private void Awake()
        {
            if (Viewport == null)
            {
                Viewport = PixelViewportManager.Instance != null
                    ? PixelViewportManager.Instance
                    : PixelViewportManager.EnsureExists();
            }

            EnsureCanvasAndImage();
            EnsureDisplayBridgeCamera();
        }

        private void OnEnable()
        {
            if (Viewport != null)
            {
                Viewport.ViewportChanged += Apply;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (Viewport != null)
            {
                Viewport.ViewportChanged -= Apply;
            }
        }

        private void EnsureCanvasAndImage()
        {
            if (OutputCanvas == null)
            {
                var canvasGo = new GameObject("[ZomCity]PixelViewportOutput");
                canvasGo.transform.SetParent(transform, worldPositionStays: false);
                OutputCanvas = canvasGo.AddComponent<Canvas>();
                OutputCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                OutputCanvas.sortingOrder = -100;

                // 保证 UI 坐标与屏幕像素一比一，避免二次缩放带来的采样误差。
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;

                // 输出层不添加 Raycaster，避免拦截 UI 点击。
            }

            if (OutputImage == null)
            {
                var imgGo = new GameObject("WorldRT");
                imgGo.transform.SetParent(OutputCanvas.transform, worldPositionStays: false);

                var rt = imgGo.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;

                OutputImage = imgGo.AddComponent<RawImage>();
                OutputImage.raycastTarget = false;
            }
        }

        private void EnsureDisplayBridgeCamera()
        {
            if (DisplayBridgeCamera == null)
            {
                var existing = transform.Find(DisplayBridgeCameraName);
                if (existing != null)
                {
                    DisplayBridgeCamera = existing.GetComponent<Camera>();
                }
            }

            if (DisplayBridgeCamera == null)
            {
                var cameraGo = new GameObject(DisplayBridgeCameraName);
                cameraGo.transform.SetParent(transform, worldPositionStays: false);
                DisplayBridgeCamera = cameraGo.AddComponent<Camera>();
            }

            DisplayBridgeCamera.clearFlags = CameraClearFlags.Depth;
            DisplayBridgeCamera.backgroundColor = Color.clear;
            DisplayBridgeCamera.cullingMask = 0;
            DisplayBridgeCamera.depth = -1000f;
            DisplayBridgeCamera.allowHDR = false;
            DisplayBridgeCamera.allowMSAA = false;
            DisplayBridgeCamera.nearClipPlane = 0.01f;
            DisplayBridgeCamera.farClipPlane = 10f;
            DisplayBridgeCamera.enabled = true;
        }

        private void Apply()
        {
            if (Viewport == null || OutputImage == null)
            {
                return;
            }

            OutputImage.texture = Viewport.WorldRenderTexture;

            if (OutputImage.rectTransform == null)
            {
                return;
            }

            var rect = Viewport.DisplayRect;
            OutputImage.rectTransform.anchoredPosition = new Vector2(rect.x, rect.y);
            OutputImage.rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
        }
    }
}
