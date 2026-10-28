using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Camera-space guidance for bloom, or an opt-in overlay above scene post-processing.</summary>
    [ExecuteAlways, RequireComponent(typeof(Canvas))]
    public sealed class HandGuidanceCanvas : MonoBehaviour
    {
        [Tooltip("Keep the prompt legible above the restoration darkness without revealing any coverage.")]
        public bool renderAfterPostProcessing;
        private Canvas targetCanvas;

        private void OnEnable() { BindCamera(); }
        private void LateUpdate() { BindCamera(); }

        private void BindCamera()
        {
            if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
            if (renderAfterPostProcessing)
            {
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                return;
            }
            if (targetCanvas.renderMode != RenderMode.ScreenSpaceCamera)
                targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (targetCanvas.worldCamera == null) targetCanvas.worldCamera = Camera.main;
            var camera = targetCanvas.worldCamera;
            if (camera != null)
                targetCanvas.planeDistance = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, 0.02f);
        }
    }
}
