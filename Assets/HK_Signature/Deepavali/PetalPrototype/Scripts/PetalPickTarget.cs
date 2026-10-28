using System.Collections.Generic;
using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Exact, on-demand picking of a small skinned petal; no per-frame mesh baking.</summary>
    [DisallowMultipleComponent]
    public sealed class PetalPickTarget : MonoBehaviour
    {
        [SerializeField] private PetalMotionController motion;
        [SerializeField] private SkinnedMeshRenderer surface;

        private Mesh bakedMesh;
        private int[] triangles;
        private readonly List<Vector3> vertices = new List<Vector3>(256);

        public PetalMotionController Motion => motion;
        public SkinnedMeshRenderer Surface => surface;

        public bool TryRaycast(Ray worldRay, out float distance)
        {
            distance = float.PositiveInfinity;
            if (!isActiveAndEnabled || motion == null || !motion.isActiveAndEnabled ||
                surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy ||
                surface.sharedMesh == null || !surface.bounds.IntersectRay(worldRay))
                return false;

            if (bakedMesh == null)
            {
                bakedMesh = new Mesh { name = "Petal picking snapshot", hideFlags = HideFlags.DontSave };
                bakedMesh.MarkDynamic();
            }

            // Handle the renderer's scale during baking; the ray below is already
            // transformed into renderer-local space, including the flower root scale.
            surface.BakeMesh(bakedMesh, true);
            if (triangles == null) triangles = bakedMesh.triangles;
            bakedMesh.GetVertices(vertices);

            Matrix4x4 inverse = surface.transform.worldToLocalMatrix;
            Vector3 origin = inverse.MultiplyPoint3x4(worldRay.origin);
            // Keep this unnormalised so the intersection parameter remains a world-space distance.
            Vector3 direction = inverse.MultiplyVector(worldRay.direction);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 edge1 = vertices[triangles[i + 1]] - a;
                Vector3 edge2 = vertices[triangles[i + 2]] - a;
                Vector3 cross = Vector3.Cross(direction, edge2);
                float determinant = Vector3.Dot(edge1, cross);
                if (Mathf.Abs(determinant) < 1e-8f) continue;

                float inverseDeterminant = 1f / determinant;
                Vector3 offset = origin - a;
                float u = Vector3.Dot(offset, cross) * inverseDeterminant;
                if (u < 0f || u > 1f) continue;

                Vector3 q = Vector3.Cross(offset, edge1);
                float v = Vector3.Dot(direction, q) * inverseDeterminant;
                if (v < 0f || u + v > 1f) continue;

                float hitDistance = Vector3.Dot(edge2, q) * inverseDeterminant;
                if (hitDistance >= 0f && hitDistance < distance) distance = hitDistance;
            }

            return !float.IsPositiveInfinity(distance);
        }

        private void OnDestroy()
        {
            if (bakedMesh == null) return;
            if (Application.isPlaying) Destroy(bakedMesh);
            else DestroyImmediate(bakedMesh);
        }
    }
}
