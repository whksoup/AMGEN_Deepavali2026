using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Rotation-only motion for a connected, three-bone petal spine.</summary>
    [DisallowMultipleComponent]
    public sealed class PetalMotionController : MonoBehaviour
    {
        [Header("Imported spine, in root-to-tip order")]
        [SerializeField] private Transform baseBone;
        [SerializeField] private Transform middleBone;
        [SerializeField] private Transform tipBone;

        [Header("Idle — degrees per bone")]
        [SerializeField] private Vector3 liftBias = new Vector3(1.5f, 2f, 2f);
        [SerializeField] private Vector3 idleLift = new Vector3(1f, 1.4f, 1.8f);
        [SerializeField] private Vector3 idleSway = new Vector3(0.8f, 1.2f, 1.5f);
        [SerializeField, Min(0.01f)] private float idleFrequency = 0.18f;
        [SerializeField, Range(0f, 6.283185f)] private float phaseOffset;

        [Header("Pulse — adds to the current idle pose")]
        [SerializeField] private Vector3 pulseLift = new Vector3(5f, 9f, 8f);
        [SerializeField, Min(0.01f)] private float pulseHold = 0.18f;
        [SerializeField, Min(0.01f)] private float pulseAttack = 0.12f;
        [SerializeField, Min(0.01f)] private float pulseRelease = 0.32f;

        private Quaternion baseRest;
        private Quaternion middleRest;
        private Quaternion tipRest;
        private bool hasRestPose;
        private float liftPhase;
        private float swayPhase;
        private float idleFade;
        private float holdRemaining;
        private float pulseWeight;
        private float pulseVelocity;
        private Vector3 currentLiftDegrees;
        private Vector3 currentSwayDegrees;

        public float PulseWeight => pulseWeight;
        public Vector3 CurrentLiftDegrees => currentLiftDegrees;
        public Vector3 CurrentSwayDegrees => currentSwayDegrees;

        private void OnEnable()
        {
            if (!hasRestPose)
            {
                if (baseBone == null || middleBone == null || tipBone == null ||
                    baseBone == middleBone || middleBone == tipBone || baseBone == tipBone ||
                    middleBone.parent != baseBone || tipBone.parent != middleBone)
                {
                    Debug.LogError("Assign the three connected petal bones in root-to-tip order.", this);
                    enabled = false;
                    return;
                }

                baseRest = baseBone.localRotation;
                middleRest = middleBone.localRotation;
                tipRest = tipBone.localRotation;
                hasRestPose = true;
            }

            liftPhase = swayPhase = phaseOffset;
            idleFade = holdRemaining = pulseWeight = pulseVelocity = 0f;
            RestoreRestPose();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Independent wrapped phases avoid growing clocks and discontinuities.
            liftPhase = Mathf.Repeat(liftPhase + dt * idleFrequency * 2f * Mathf.PI, 2f * Mathf.PI);
            swayPhase = Mathf.Repeat(swayPhase + dt * idleFrequency * 0.67f * 2f * Mathf.PI, 2f * Mathf.PI);
            idleFade = Mathf.MoveTowards(idleFade, 1f, dt * 2f);

            float target = holdRemaining > 0f ? 1f : 0f;
            holdRemaining = Mathf.Max(0f, holdRemaining - dt);
            pulseWeight = Mathf.SmoothDamp(pulseWeight, target, ref pulseVelocity,
                target > pulseWeight ? pulseAttack : pulseRelease, Mathf.Infinity, dt);

            ApplyBone(baseBone, baseRest, 0);
            ApplyBone(middleBone, middleRest, 1);
            ApplyBone(tipBone, tipRest, 2);
        }

        private void ApplyBone(Transform bone, Quaternion rest, int index)
        {
            float lift = idleFade * (liftBias[index] + idleLift[index] * Mathf.Sin(liftPhase - index * 0.35f));
            lift += pulseWeight * pulseLift[index];
            float sway = idleFade * idleSway[index] * Mathf.Sin(swayPhase - index * 0.45f);
            currentLiftDegrees[index] = lift;
            currentSwayDegrees[index] = sway;

            // Verified imported FBX axes: local X lifts; local Z sways.
            // Never accumulate rotations or change bone positions/scales: the base stays pinned.
            bone.localRotation = rest * Quaternion.AngleAxis(lift, Vector3.right)
                                      * Quaternion.AngleAxis(sway, Vector3.forward);
        }

        /// <summary>Callable by UnityEvents or the future interaction/session controller.</summary>
        [ContextMenu("Trigger Pulse (Play Mode)")]
        public void TriggerPulse()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !hasRestPose) return;
            // Extend the pulse without snapping the current pose or stacking unbounded angles.
            holdRemaining = pulseHold;
        }

        private void OnDisable()
        {
            RestoreRestPose();
            holdRemaining = pulseWeight = pulseVelocity = idleFade = 0f;
        }

        private void RestoreRestPose()
        {
            currentLiftDegrees = currentSwayDegrees = Vector3.zero;
            if (!hasRestPose) return;
            if (baseBone != null) baseBone.localRotation = baseRest;
            if (middleBone != null) middleBone.localRotation = middleRest;
            if (tipBone != null) tipBone.localRotation = tipRest;
        }

        private void OnValidate()
        {
            idleFrequency = Mathf.Max(0.01f, idleFrequency);
            pulseHold = Mathf.Max(0.01f, pulseHold);
            pulseAttack = Mathf.Max(0.01f, pulseAttack);
            pulseRelease = Mathf.Max(0.01f, pulseRelease);
        }
    }
}
