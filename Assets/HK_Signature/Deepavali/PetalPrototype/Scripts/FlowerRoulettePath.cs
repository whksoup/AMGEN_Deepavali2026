using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Monotonic, frame-rate-independent clockwise travel with zero starting/ending speed.</summary>
    public static class FlowerRoulettePath
    {
        public static float Position(float normalizedTime, int turns, int winnerSlot, int count = 8)
        {
            float t = Mathf.Clamp01(normalizedTime);
            float eased = t * t * (3 - 2 * t);
            return (Mathf.Max(1, turns) * count + winnerSlot) * eased;
        }

        public static int Slot(float normalizedTime, int turns, int winnerSlot, int count = 8) =>
            Mathf.FloorToInt(Position(normalizedTime, turns, winnerSlot, count) + .5f) % count;

        /// <summary>Crossfade adjacent tips continuously, including the last-to-first wrap.</summary>
        public static float TipWeight(float position, int slot, int count = 8)
        {
            if (slot < 0 || slot >= count) return 0;
            float distance = Mathf.Abs(Mathf.Repeat(position, count) - slot);
            distance = Mathf.Min(distance, count - distance);
            // Equivalent to lerping the outgoing tip from 1 to 0 and the incoming
            // tip from 0 to 1 with an eased fraction. Weights sum to one.
            return Mathf.Lerp(1, 0, Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance)));
        }
    }
}
