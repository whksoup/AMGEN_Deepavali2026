using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Keeps the corner lamps and border dressing composed for the wall's aspect ratio.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class DeepavaliBackground : MonoBehaviour
    {
        [Serializable]
        public struct BorderPiece
        {
            public Transform transform;
            public Vector2 viewport;
            public float clearance;
        }

        public Transform[] lamps = Array.Empty<Transform>();
        public Transform[] flames = Array.Empty<Transform>();
        public Light[] lampLights = Array.Empty<Light>();
        public BorderPiece[] border = Array.Empty<BorderPiece>();
        public Renderer soil;
        public float floorHeight = -0.08f;
        [Min(0)] public float flowerClearance = 0.52f;
        [Range(0.04f, 0.25f)] public float cornerInsetX = 0.085f;
        [Range(0.06f, 0.3f)] public float cornerInsetY = 0.16f;

        private MaterialPropertyBlock soilProperties;
        private Matrix4x4 lastCameraMatrix, lastProjection;
        private bool laidOut;
        private static readonly int ViewBounds = Shader.PropertyToID("_ViewBounds");
        private static readonly int[] LampPositions = {
            Shader.PropertyToID("_Lamp0"), Shader.PropertyToID("_Lamp1"),
            Shader.PropertyToID("_Lamp2"), Shader.PropertyToID("_Lamp3") };

        private void OnEnable() { laidOut = false; }
        private void OnValidate() { laidOut = false; }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null) return;
            if (!laidOut || lastCameraMatrix != camera.cameraToWorldMatrix || lastProjection != camera.projectionMatrix)
                RefreshLayout(camera);
            if (!Application.isPlaying) return;
            for (int i = 0; i < lampLights.Length; i++)
                if (lampLights[i] != null)
                    lampLights[i].intensity = 0.005f * (0.94f + 0.06f * Mathf.Sin(Time.time * 5.7f + i * 2.1f)
                        + 0.025f * Mathf.Sin(Time.time * 13.3f + i));
        }

        public void RefreshLayout(Camera camera)
        {
            var plane = new Plane(Vector3.up, new Vector3(0, floorHeight, 0));
            Vector3 Project(Vector2 point)
            {
                Ray ray = camera.ViewportPointToRay(point);
                return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : transform.position;
            }
            var lower = Project(Vector2.zero);
            var upper = Project(Vector2.one);
            var center = Project(new Vector2(0.5f, 0.5f));
            float halfWidth = Mathf.Abs(upper.x - lower.x) * 0.5f;
            float halfHeight = Mathf.Abs(upper.z - lower.z) * 0.5f;
            var anchors = new[] {
                new Vector2(cornerInsetX, 1 - cornerInsetY), new Vector2(1 - cornerInsetX, 1 - cornerInsetY),
                new Vector2(cornerInsetX, cornerInsetY), new Vector2(1 - cornerInsetX, cornerInsetY) };
            soilProperties ??= new MaterialPropertyBlock();
            if (soil != null) soil.GetPropertyBlock(soilProperties);
            soilProperties.SetVector(ViewBounds, new Vector4(center.x, center.z, halfWidth, halfHeight));
            for (int i = 0; i < lamps.Length && i < 4; i++)
            {
                if (lamps[i] == null) continue;
                lamps[i].position = Project(anchors[i]);
                soilProperties.SetVector(LampPositions[i], lamps[i].position);
            }
            if (soil != null) soil.SetPropertyBlock(soilProperties);
            foreach (var piece in border)
            {
                if (piece.transform == null) continue;
                Vector3 point = Project(piece.viewport);
                bool visible = new Vector2(point.x, point.z).magnitude > flowerClearance + piece.clearance;
                foreach (var lamp in lamps)
                    if (lamp != null && Vector3.Distance(point, lamp.position) < 0.105f + piece.clearance) visible = false;
                piece.transform.gameObject.SetActive(visible);
                piece.transform.position = point + Vector3.up * 0.002f;
            }
            foreach (var flame in flames)
                if (flame != null) flame.rotation = camera.transform.rotation;
            lastCameraMatrix = camera.cameraToWorldMatrix;
            lastProjection = camera.projectionMatrix;
            laidOut = true;
        }
    }
}
