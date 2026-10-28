using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Connects the reusable screen-space canvas to the scene camera, before post-processing.</summary>
    [ExecuteAlways, RequireComponent(typeof(Canvas))]
    public sealed class HandGuidanceCanvas : MonoBehaviour
    {
        private Canvas targetCanvas;

        private void OnEnable() { BindCamera(); }
        private void LateUpdate() { BindCamera(); }

        private void BindCamera()
        {
            if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
            if (targetCanvas.renderMode != RenderMode.ScreenSpaceCamera)
                targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (targetCanvas.worldCamera == null) targetCanvas.worldCamera = Camera.main;
            var camera = targetCanvas.worldCamera;
            if (camera != null)
                targetCanvas.planeDistance = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, 0.02f);
        }
    }
}
