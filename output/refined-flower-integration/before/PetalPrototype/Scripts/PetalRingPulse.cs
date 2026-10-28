using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Local rustle from direct pulses, resolved before the lower layer follows.</summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PetalRingPulse : MonoBehaviour
    {
        [Tooltip("Upper petals in clockwise order as seen from the interaction camera.")]
        [SerializeField] private PetalMotionController[] clockwisePetals;
        [SerializeField, Range(0f, 1f)] private float firstClockwise = 0.55f;
        [SerializeField, Range(0f, 1f)] private float secondClockwise = 0.22f;
        [SerializeField, Range(0f, 1f)] private float anticlockwiseRustle = 0.12f;

        private void OnEnable()
        {
            if (clockwisePetals == null || clockwisePetals.Length < 4)
            {
                FailSetup();
                return;
            }
            for (int i = 0; i < clockwisePetals.Length; i++)
            {
                if (clockwisePetals[i] == null) { FailSetup(); return; }
                for (int j = 0; j < i; j++)
                    if (clockwisePetals[i] == clockwisePetals[j]) { FailSetup(); return; }
            }
        }

        private void FailSetup()
        {
            Debug.LogError("Assign at least four unique upper petals in clockwise order.", this);
            enabled = false;
        }

        private void LateUpdate() => SynchronizePose();

        /// <summary>Uses only direct pulses, so simultaneous taps cannot amplify or circulate a rustle.</summary>
        public void SynchronizePose()
        {
            if (!isActiveAndEnabled) return;
            int count = clockwisePetals.Length;
            for (int i = 0; i < count; i++)
            {
                var petal = clockwisePetals[i];
                if (petal == null) continue;
                float follow = Mathf.Max(
                    DirectPulse((i + count - 1) % count) * firstClockwise,
                    DirectPulse((i + count - 2) % count) * secondClockwise);
                follow = Mathf.Max(follow, DirectPulse((i + 1) % count) * anticlockwiseRustle);
                // Match the source's rise AND release; no delay leaves the upper neighbor behind.
                petal.ApplyFollowPulse(follow);
            }
        }

        private float DirectPulse(int index)
        {
            var petal = clockwisePetals[index];
            return petal != null && petal.isActiveAndEnabled ? petal.PulseWeight : 0f;
        }

        private void OnDisable()
        {
            if (clockwisePetals == null) return;
            foreach (var petal in clockwisePetals)
                if (petal != null) petal.ApplyFollowPulse(0f);
        }
    }
}
