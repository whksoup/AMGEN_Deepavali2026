using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Persistent, linear coverage shared by rendering, progress and scene responses.</summary>
    public sealed class RestorationMask
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; }
        public float Coverage => coverageCount > 0 ? (float)(coverageSum / coverageCount) : 0;
        private readonly float[] values;
        private readonly bool[] counted;
        private readonly int coverageCount;
        private double coverageSum;

        public RestorationMask(int width, int height, Rect completionArea)
        {
            Width = Mathf.Max(2, width); Height = Mathf.Max(2, height);
            Pixels = new byte[Width * Height]; values = new float[Pixels.Length];
            counted = new bool[Pixels.Length];
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                if (completionArea.Contains(new Vector2((x + .5f) / Width, (y + .5f) / Height)))
                { counted[y * Width + x] = true; coverageCount++; }
        }

        public void Clear()
        {
            Array.Clear(values, 0, values.Length); Array.Clear(Pixels, 0, Pixels.Length);
            coverageSum = 0;
        }

        public static bool IsValidPoint(Vector2 p) => float.IsFinite(p.x) && float.IsFinite(p.y)
            && p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1;

        /// <summary>Paint a capsule between samples, with radius measured in viewport heights.</summary>
        public void Paint(Vector2 from, Vector2 to, float radius, float softness, float amount, float aspect)
        {
            if (!IsValidPoint(from) || !IsValidPoint(to) || !float.IsFinite(amount) || amount <= 0 ||
                !float.IsFinite(radius) || radius <= 0 || !float.IsFinite(aspect) || aspect <= 0) return;
            float rx = radius / aspect;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(from.x, to.x) - rx) * Width), 0, Width - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(from.x, to.x) + rx) * Width), 0, Width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(from.y, to.y) - radius) * Height), 0, Height - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(from.y, to.y) + radius) * Height), 0, Height - 1);
            Vector2 a = new Vector2(from.x * aspect, from.y), b = new Vector2(to.x * aspect, to.y);
            Vector2 ab = b - a;
            float inner = radius * (1 - Mathf.Clamp(softness, .01f, 1));
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2((x + .5f) / Width * aspect, (y + .5f) / Height);
                float t = ab.sqrMagnitude > .0000001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
                float d = Vector2.Distance(p, a + ab * t);
                float weight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(inner, radius, d));
                int i = y * Width + x;
                float next = Mathf.Clamp01(values[i] + weight * amount);
                if (counted[i]) coverageSum += next - values[i];
                values[i] = next; Pixels[i] = (byte)Mathf.RoundToInt(next * 255);
            }
        }

        public float Sample(Vector2 uv)
        {
            if (!IsValidPoint(uv)) return 0;
            float x = Mathf.Clamp(uv.x * Width - .5f, 0, Width - 1);
            float y = Mathf.Clamp(uv.y * Height - .5f, 0, Height - 1);
            int ix = (int)x, iy = (int)y, nx = Mathf.Min(ix + 1, Width - 1), ny = Mathf.Min(iy + 1, Height - 1);
            return Mathf.Lerp(Mathf.Lerp(values[iy * Width + ix], values[iy * Width + nx], x - ix),
                Mathf.Lerp(values[ny * Width + ix], values[ny * Width + nx], x - ix), y - iy);
        }
    }
}
