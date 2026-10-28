using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Amgen.Deepavali
{
    /// <summary>Resolution-independent hand guidance. Use on a Screen Space - Camera canvas for HDR bloom.</summary>
    [AddComponentMenu("UI/Deepavali/Hand Here")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HandHereGraphic : MaskableGraphic
    {
        [SerializeField, Range(16, 80)] private int dotCount = 48;
        [SerializeField, Range(0.5f, 4f)] private float handStroke = 2f;
        [SerializeField, Range(0f, 2f)] private float ringBrightness = 0.35f;
        [SerializeField, Range(0f, 2f)] private float dotBrightness = 0.45f;
        private readonly List<Vector2> outline = new List<Vector2>(200);
        private Vector2 center;
        private float scale;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = true;
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            return ContainsScreenPoint(screenPoint, eventCamera) && base.Raycast(screenPoint, eventCamera);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local)) return false;
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 99f / 220f;
            return (local - rect.center).sqrMagnitude <= radius * radius;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            center = rect.center;
            scale = Mathf.Min(rect.width, rect.height) / 220f;
            // Ten design pixels of breathing room for the bloom around the disc.
            Disc(vh, new Vector2(100, 100), 99, 128, 0);
            Ring(vh, 98.5f, 1f, ringBrightness);
            for (int i = 0; i < dotCount; i++)
            {
                float angle = i * Mathf.PI * 2f / dotCount;
                bool cardinal = i % (dotCount / 4) == 0;
                Disc(vh, new Vector2(100 + Mathf.Sin(angle) * 83, 100 + Mathf.Cos(angle) * 83),
                    cardinal ? 2.15f : 1.25f, 12, dotBrightness, true);
            }

            outline.Clear();
            outline.Add(new Vector2(95, 143));
            Curve(87,143, 84,137, 81,131);
            Curve(77,122, 72,113, 66,104);
            Curve(62,98, 64,94, 68,95);
            Curve(73,96, 77,105, 81,107);
            Curve(84,109, 85,105, 84,100);
            Curve(82,88, 78,72, 77,62);
            Curve(76,55, 82,53, 84,60);
            Curve(87,69, 89,81, 91,89);
            Curve(92,93, 96,93, 96,88);
            Curve(96,77, 94,62, 95,54);
            Curve(95,47, 102,47, 103,54);
            Curve(104,64, 103,78, 104,87);
            Curve(104,92, 108,92, 109,87);
            Curve(111,78, 111,66, 114,60);
            Curve(117,53, 122,56, 121,63);
            Curve(120,74, 117,87, 117,94);
            Curve(117,99, 120,100, 122,95);
            Curve(125,89, 127,82, 131,80);
            Curve(136,77, 139,81, 136,87);
            Curve(131,99, 128,109, 126,120);
            Curve(124,131, 122,140, 116,143);
            Curve(110,145, 102,143, 95,143);
            for (int i = 1; i < outline.Count; i++)
                Stroke(vh, outline[i - 1], outline[i], handStroke, 1);
            // Round joins avoid sharp corners at the fingertips and thumb.
            for (int i = 0; i < outline.Count; i++)
                Disc(vh, outline[i], handStroke * 0.5f, 8, 1);

            // Small independent quads drift in the vertex shader as the badge dissolves.
            for (int i = 0; i < outline.Count; i += 3) Particle(vh, outline[i]);
            for (int i = 0; i < dotCount; i++)
            {
                float angle = i * Mathf.PI * 2f / dotCount;
                Particle(vh, new Vector2(100 + Mathf.Sin(angle) * 83, 100 + Mathf.Cos(angle) * 83));
            }
        }

        private void Curve(float ax, float ay, float bx, float by, float x, float y)
        {
            Vector2 start = outline[outline.Count - 1];
            var a = new Vector2(ax, ay); var b = new Vector2(bx, by); var end = new Vector2(x, y);
            for (int i = 1; i <= 8; i++)
            {
                float t = i / 8f, u = 1 - t;
                outline.Add(u * u * u * start + 3 * u * u * t * a + 3 * u * t * t * b + t * t * t * end);
            }
        }

        private void Vertex(VertexHelper vh, Vector2 point, float brightness, bool dot = false)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = center + new Vector2(point.x - 100, 100 - point.y) * scale;
            vertex.color = color;
            // Zero is the dark backing; positive values select HDR gold.
            vertex.uv0 = new Vector4(brightness, dot ? 1 : 0, point.x / 200f, point.y / 200f);
            vh.AddVert(vertex);
        }

        private void Disc(VertexHelper vh, Vector2 point, float radius, int segments, float brightness, bool dot = false)
        {
            int start = vh.currentVertCount;
            Vertex(vh, point, brightness, dot);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                Vertex(vh, point + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, brightness, dot);
                if (i > 0) vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        private void Particle(VertexHelper vh, Vector2 point)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < 4; i++)
            {
                float x = i == 0 || i == 3 ? -1 : 1;
                float y = i < 2 ? -1 : 1;
                var vertex = UIVertex.simpleVert;
                vertex.position = center + new Vector2(point.x - 100, 100 - point.y) * scale;
                vertex.color = color;
                // Negative X marks particles, with corner X packed as -2/-3.
                vertex.uv0 = new Vector4(x < 0 ? -2 : -3, y, point.x / 200f, point.y / 200f);
                vh.AddVert(vertex);
            }
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        private void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, float brightness)
        {
            Vector2 delta = (b - a).normalized;
            Vector2 normal = new Vector2(-delta.y, delta.x) * width * 0.5f;
            int start = vh.currentVertCount;
            Vertex(vh, a - normal, brightness); Vertex(vh, a + normal, brightness);
            Vertex(vh, b + normal, brightness); Vertex(vh, b - normal, brightness);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        private void Ring(VertexHelper vh, float radius, float width, float brightness)
        {
            for (int i = 0; i < 160; i++)
            {
                float a = i * Mathf.PI * 2 / 160, b = (i + 1) * Mathf.PI * 2 / 160;
                Stroke(vh, new Vector2(100 + Mathf.Cos(a) * radius, 100 + Mathf.Sin(a) * radius),
                    new Vector2(100 + Mathf.Cos(b) * radius, 100 + Mathf.Sin(b) * radius), width, brightness);
            }
        }
    }
}
