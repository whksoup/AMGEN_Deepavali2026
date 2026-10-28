using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    public enum PetalWheelMode { Shimmer, Pulse, Settle, Ready, Spin, Tease, Win }
    public enum PetalWheelOuterMotion { Breathe, Chase }
    public enum PetalWheelWinPattern { Breathe, Swell, Burst, Alternate, BurstFlash, Sparkle }
    public enum PetalWheelAfterHold { Ready, Shimmer, Stay }

    [Serializable]
    public sealed class PetalWheelTiming
    {
        [Header("Shimmer and entry (seconds)")]
        [Min(.1f)] public float shimmerPeriod = 5.5f;
        [Range(0, 1)] public float shimmerLevel = .6f, shimmerVariation = .5f;
        [Min(.1f)] public float pulseLength = .8f, settleLength = 1.6f;
        [Header("Wheel motion")]
        [Min(.1f)] public float speed = 16;
        [Min(0)] public float cruise = 1.2f;
        [Min(.1f)] public float slowdown = 3.6f;
        [Min(0)] public float idleSpeed = 1.2f;
        public bool windup, overshoot;
        [Header("Light envelopes (seconds)")]
        [Min(0)] public float fadeIn = .16f, innerAfterglow = .45f, outerAfterglow = .42f;
        [Min(.1f)] public float breathPeriod = 4.5f;
        [Range(0, 1)] public float breathVariation = .4f;
        [Min(.01f)] public float lightUp = .5f;
        public PetalWheelOuterMotion outerMotion;
        [Min(.1f)] public float outerTickRatio = 2.5f;
        public bool counterClockwise;
        [Range(1, 4)] public int chaseHeads = 1;
        [Range(0, 1)] public float neighborSpill = .35f;
        [Header("Result")]
        public PetalWheelWinPattern winPattern;
        public bool bloom = true;
        [Min(.01f)] public float bloomDelay = 1.4f, bloomSpread = 1.8f, hold = 3.5f;
        public PetalWheelAfterHold afterHold;
    }

    /// <summary>Light-level and timing logic from petal-wheel.html. No materials or transforms.</summary>
    public sealed class PetalWheelPattern
    {
        public readonly PetalWheelTiming Timing;
        public readonly float[] Inner = new float[8], Outer;
        public PetalWheelMode Mode { get; private set; }
        public string Phase { get; private set; } = "Shimmer";
        public int Winner { get; private set; } = -1;
        public int Active => Mod(Mathf.FloorToInt(Position), 8);
        public float Position { get; private set; } = .5f;
        public float Velocity { get; private set; }
        public float StageTime { get; private set; }
        public float Clock { get; private set; }
        public float Hub { get; private set; }
        public float CenterIcon { get; private set; }
        public float Bloom { get; private set; }
        public float BloomAlpha { get; private set; }
        public bool HoldResult { get; set; }
        public bool Busy => Mode == PetalWheelMode.Pulse || Mode == PetalWheelMode.Settle || Mode == PetalWheelMode.Spin || Mode == PetalWheelMode.Tease;
        public float PlannedDuration => windupTime + .9f + cruiseTime + slowTime;

        private readonly float[] innerTarget = new float[8], outerTarget, fromInner = new float[8], fromOuter;
        private sealed class Drift { public float offset, frequency, phase, peak = 1; public int cycle; }
        private readonly Drift[] shimmer, breath;
        private readonly System.Random random;
        private float shimmerClock, breathClock, energy, floodStart = -1e9f, sparkTime;
        private float p0, p1, v0, vmax, windupTime, accelDistance, cruiseDistance, cruiseTime, slowDistance, slowTime, land, end;
        private bool spinAfterPulse, tease;
        private int pendingWinner = -1;
        private static float SafeTime(float t) => Mathf.Max(.0001f, t);
        public static int Mod(int n, int count) => (n % count + count) % count;
        public static float Ease(float x) { x = Mathf.Clamp01(x); return x < .5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2; }
        public static float Approach(float current, float target, float dt, float rise, float fall)
        {
            float duration = target >= current ? rise : fall;
            return duration <= 0 ? target : Mathf.MoveTowards(current, target, dt / duration);
        }
        private float Random01() => (float)random.NextDouble();

        public PetalWheelPattern(PetalWheelTiming timing, int outerCount, int seed = 137)
        {
            Timing = timing; random = new System.Random(seed);
            Outer = new float[outerCount]; outerTarget = new float[outerCount]; fromOuter = new float[outerCount];
            shimmer = new Drift[8 + outerCount]; breath = new Drift[outerCount];
            for (int i = 0; i < shimmer.Length; i++) shimmer[i] = new Drift { offset = Mathf.Repeat(i * .6180339887f, 1), frequency = .04f + .06f * Random01(), phase = Random01() * 2 * Mathf.PI };
            for (int i = 0; i < breath.Length; i++) breath[i] = new Drift { offset = Mathf.Repeat(i * .6180339887f, 1), frequency = .05f + .07f * Random01(), phase = Random01() * 2 * Mathf.PI, peak = 1 - .4f * Random01() };
        }

        private void SetMode(PetalWheelMode mode) { Mode = mode; StageTime = 0; }
        public bool EnterWheel(bool spin = false, int target = -1)
        {
            if (Mode != PetalWheelMode.Shimmer) return false;
            spinAfterPulse = spin; pendingWinner = target; SetMode(PetalWheelMode.Pulse); return true;
        }
        public bool Spin(int target = -1)
        {
            if (Busy || target < -1 || target >= 8) return false;
            if (Mode == PetalWheelMode.Shimmer) return EnterWheel(true, target);
            Winner = target < 0 ? random.Next(8) : target;
            p0 = Position; windupTime = Timing.windup ? .45f : 0; p1 = p0 - (Timing.windup ? .7f : 0);
            v0 = Timing.windup || Mode == PetalWheelMode.Win ? 0 : Mathf.Max(0, Velocity);
            vmax = Mathf.Max(.1f, Timing.speed); slowTime = SafeTime(Timing.slowdown);
            accelDistance = (v0 + vmax) * .9f / 2; slowDistance = vmax * slowTime / 3;
            tease = Timing.overshoot; float offset = tease ? 1 : 0;
            float minimum = p1 + accelDistance + vmax * Mathf.Max(0, Timing.cruise) + slowDistance;
            land = 8 * Mathf.Ceil((minimum - offset - Winner - .5f) / 8) + Winner + .5f; end = land + offset;
            cruiseDistance = end - p1 - accelDistance - slowDistance; cruiseTime = cruiseDistance / vmax;
            floodStart = Clock; SetMode(PetalWheelMode.Spin); return true;
        }
        public bool ReturnToShimmer()
        {
            if (Busy) return false;
            SetMode(PetalWheelMode.Shimmer); Velocity = 0; Winner = -1; return true;
        }
        public bool PreviewWin(int slot)
        {
            if (Busy || slot < 0 || slot >= 8) return false;
            Winner = slot; Position = slot + .5f; EnterWin(); return true;
        }
        private void EnterWin() { Velocity = 0; Bloom = 0; SetMode(PetalWheelMode.Win); }

        public void Tick(float dt)
        {
            if (!float.IsFinite(dt) || dt < 0) return;
            // Bounded substeps preserve the afterglow when the host supplies a long frame.
            while (dt > 0) { float step = Mathf.Min(dt, 1f / 120); Step(step); dt -= step; }
        }
        private void Step(float dt)
        {
            Clock += dt; StageTime += dt;
            switch (Mode)
            {
                case PetalWheelMode.Shimmer: Phase = "Shimmer"; shimmerClock += dt / SafeTime(Timing.shimmerPeriod); break;
                case PetalWheelMode.Pulse:
                    Phase = "Starting";
                    if (StageTime >= Timing.pulseLength)
                    { Array.Copy(Inner, fromInner, 8); Array.Copy(Outer, fromOuter, Outer.Length); SetMode(PetalWheelMode.Settle); }
                    break;
                case PetalWheelMode.Settle:
                    if (StageTime >= Timing.settleLength)
                    { SetMode(PetalWheelMode.Ready); if (spinAfterPulse) { Spin(pendingWinner); floodStart = Clock - Timing.lightUp; } }
                    break;
                case PetalWheelMode.Ready: Phase = "Ready"; Velocity = Timing.idleSpeed; Position += Velocity * dt; break;
                case PetalWheelMode.Spin: EvaluateSpin(); break;
                case PetalWheelMode.Tease:
                    Phase = "Settle";
                    float w = Mathf.Clamp01((StageTime - .45f) / .4f);
                    float ease = w < .5f ? 2 * w * w : 1 - Mathf.Pow(-2 * w + 2, 2) / 2;
                    Position = end - ease; Velocity = StageTime < .45f ? 0 : -2.5f;
                    if (w >= 1) { Position = land; EnterWin(); }
                    break;
                case PetalWheelMode.Win:
                    Bloom = Timing.bloom ? Mathf.Clamp01((StageTime - Timing.bloomDelay) / SafeTime(Timing.bloomSpread)) : 0;
                    Phase = StageTime < Timing.bloomDelay ? "Win" : Timing.bloom && Bloom < 1 ? "Bloom" : "Hold";
                    if (!HoldResult && Timing.afterHold != PetalWheelAfterHold.Stay && StageTime > Timing.bloomDelay + Timing.bloomSpread + Timing.hold)
                    { if (Timing.afterHold == PetalWheelAfterHold.Shimmer) ReturnToShimmer(); else SetMode(PetalWheelMode.Ready); }
                    break;
            }
            bool calm = Mode == PetalWheelMode.Ready || Mode == PetalWheelMode.Shimmer || Mode == PetalWheelMode.Pulse || Mode == PetalWheelMode.Settle;
            BloomAlpha = Mode == PetalWheelMode.Win ? 1 : Mathf.Max(0, BloomAlpha - dt / (calm ? 1.4f : .6f));
            energy = Approach(energy, calm ? 0 : 1, dt, 1, 1.8f);
            breathClock += dt / SafeTime(Timing.breathPeriod * (1 - .25f * energy));
            RenderLevels(dt);
        }
        private void EvaluateSpin()
        {
            float u = StageTime;
            if (u < windupTime)
            { float w = u / windupTime; Position = p0 - .7f * (1 - Mathf.Cos(Mathf.PI * w)) / 2; Velocity = -.7f * Mathf.PI / (2 * windupTime) * Mathf.Sin(Mathf.PI * w); Phase = "Wind-up"; return; }
            u -= windupTime;
            if (u < .9f) { float a = (vmax - v0) / .9f; Position = p1 + v0 * u + .5f * a * u * u; Velocity = v0 + a * u; Phase = "Spin-up"; return; }
            u -= .9f;
            if (u < cruiseTime) { Position = p1 + accelDistance + vmax * u; Velocity = vmax; Phase = "Cruise"; return; }
            u -= cruiseTime;
            if (u < slowTime) { float w = u / slowTime; Position = p1 + accelDistance + cruiseDistance + slowDistance * (1 - Mathf.Pow(1 - w, 3)); Velocity = vmax * Mathf.Pow(1 - w, 2); Phase = "Slowing"; return; }
            Position = end; Velocity = 0; if (tease) SetMode(PetalWheelMode.Tease); else EnterWin();
        }
        private float Oscillate(Drift d, float clock, float drift, float variation, float power)
        {
            float phase = clock + d.offset + drift * Mathf.Sin(Clock * d.frequency * 2 * Mathf.PI + d.phase);
            int cycle = Mathf.FloorToInt(phase);
            if (cycle != d.cycle) { d.cycle = cycle; d.peak = 1 - variation * Random01(); }
            return d.peak * Mathf.Pow(.5f - .5f * Mathf.Cos(2 * Mathf.PI * phase), power);
        }
        private void LightOuter(int i, float value)
        {
            int n = Outer.Length; i = Mod(i, n); outerTarget[i] = Mathf.Max(outerTarget[i], value);
            outerTarget[Mod(i - 1, n)] = Mathf.Max(outerTarget[Mod(i - 1, n)], value * Timing.neighborSpill);
            outerTarget[Mod(i + 1, n)] = Mathf.Max(outerTarget[Mod(i + 1, n)], value * Timing.neighborSpill);
        }
        private void RenderLevels(float dt)
        {
            Array.Clear(innerTarget, 0, 8); Array.Clear(outerTarget, 0, Outer.Length); Hub = .12f;
            float lo = .04f + .41f * energy, hi = .55f + .45f * energy;
            for (int j = 0; j < Outer.Length; j++) outerTarget[j] = lo + (hi - lo) * Oscillate(breath[j], breathClock, Timing.breathVariation * .25f, Timing.breathVariation, 1);
            float pulse = .5f + .5f * Mathf.Sin(StageTime * 2 * Mathf.PI * 1.1f);
            if (Mode == PetalWheelMode.Win)
            {
                float keep = 1 - Bloom; innerTarget[Winner] = (.8f + .2f * pulse) * keep; Hub = .35f + .35f * pulse;
                var pattern = Timing.winPattern;
                if (pattern == PetalWheelWinPattern.BurstFlash) pattern = StageTime < 1.3f ? PetalWheelWinPattern.Burst : PetalWheelWinPattern.Alternate;
                if (pattern == PetalWheelWinPattern.Burst || pattern == PetalWheelWinPattern.Sparkle) Array.Clear(outerTarget, 0, Outer.Length);
                for (int j = 0; j < Outer.Length; j++)
                {
                    if (pattern == PetalWheelWinPattern.Alternate) outerTarget[j] = j % 2 == Mathf.FloorToInt(StageTime * 7) % 2 ? 1 : .1f;
                    if (pattern == PetalWheelWinPattern.Swell) outerTarget[j] = .45f + .4f * (j % 2 == 0 ? 1 : -1) * Mathf.Sin(StageTime * 2 * Mathf.PI * .6f);
                    outerTarget[j] *= keep;
                }
                if (pattern == PetalWheelWinPattern.Burst)
                { int d = Mathf.FloorToInt(StageTime * 14) % (Outer.Length / 2 + 1), start = Mathf.RoundToInt(Winner * Outer.Length / 8f); if (d < Outer.Length / 2) { LightOuter(start + d, keep); LightOuter(start - 1 - d, keep); } }
                if (pattern == PetalWheelWinPattern.Sparkle && Clock - sparkTime > .06f && keep > .5f)
                { sparkTime = Clock; for (int n = 0; n < 3; n++) Outer[random.Next(Outer.Length)] = 1; }
            }
            else if (Mode == PetalWheelMode.Shimmer)
            {
                for (int i = 0; i < 8; i++) innerTarget[i] = .04f + .7f * Timing.shimmerLevel * Oscillate(shimmer[i], shimmerClock, .2f, Timing.shimmerVariation, 1.8f);
                for (int j = 0; j < Outer.Length; j++) outerTarget[j] = .06f + Timing.shimmerLevel * Oscillate(shimmer[j + 8], shimmerClock, .2f, Timing.shimmerVariation, 1.8f);
                Hub = .06f + .14f * (.5f - .5f * Mathf.Cos(2 * Mathf.PI * shimmerClock * .5f)) * Timing.shimmerLevel;
            }
            else if (Mode == PetalWheelMode.Pulse)
            {
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(StageTime / SafeTime(Timing.pulseLength))), 2);
                for (int i = 0; i < 8; i++) innerTarget[i] = .06f + .47f * env;
                for (int j = 0; j < Outer.Length; j++) outerTarget[j] = .06f + .47f * env;
                Hub = .08f + .25f * env;
            }
            else
            {
                innerTarget[Active] = 1;
                if (Mode == PetalWheelMode.Settle) { if (spinAfterPulse) for (int j = 0; j < Outer.Length; j++) outerTarget[j] = 1; }
                else if (Timing.outerMotion == PetalWheelOuterMotion.Chase)
                {
                    Array.Clear(outerTarget, 0, Outer.Length);
                    int head = Mathf.FloorToInt(Position * Timing.outerTickRatio * (Timing.counterClockwise ? -1 : 1));
                    int heads = Mathf.Clamp(Timing.chaseHeads, 1, 4);
                    for (int h = 0; h < heads; h++) LightOuter(head + h * Outer.Length / heads, 1);
                }
                else
                {
                    float u = Clock - floodStart, up = SafeTime(Timing.lightUp);
                    float flood = u < up ? 1 - Mathf.Pow(1 - u / up, 2) : Mathf.Clamp01(1 - (u - up - .4f) / 1.6f);
                    for (int j = 0; j < Outer.Length; j++) outerTarget[j] = Mathf.Max(outerTarget[j], flood);
                }
            }
            float settle = Ease(StageTime / SafeTime(Timing.settleLength));
            for (int i = 0; i < 8; i++) Inner[i] = Level(Inner[i], innerTarget[i], fromInner[i], settle, dt, Mode == PetalWheelMode.Win ? Mathf.Max(.25f, Timing.innerAfterglow) : Timing.innerAfterglow);
            for (int j = 0; j < Outer.Length; j++) Outer[j] = Level(Outer[j], outerTarget[j], fromOuter[j], settle, dt, Mode == PetalWheelMode.Win && Timing.winPattern == PetalWheelWinPattern.Sparkle ? Mathf.Max(.22f, Timing.outerAfterglow) : Timing.outerAfterglow);
            CenterIcon = Mode == PetalWheelMode.Win ? Hub * (1 - Bloom) : Mode == PetalWheelMode.Shimmer || Mode == PetalWheelMode.Pulse ? Hub * 1.6f : Mode == PetalWheelMode.Settle ? .13f * (1 - settle) : 0;
        }
        private float Level(float value, float target, float from, float settle, float dt, float fade) =>
            Mode == PetalWheelMode.Pulse ? target : Mode == PetalWheelMode.Settle ? Mathf.Lerp(from, target, settle) : Approach(value, target, dt, Timing.fadeIn, fade);
    }
}
