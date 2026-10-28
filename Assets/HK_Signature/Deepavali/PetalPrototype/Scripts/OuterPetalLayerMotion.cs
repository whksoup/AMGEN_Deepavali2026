using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>One coherent lower-layer pose, bounded by every upper petal's current lift.</summary>
    [DisallowMultipleComponent]
    public sealed class OuterPetalLayerMotion : MonoBehaviour
    {
        [Tooltip("All eight top petals. Read after the ring's follow-through; no separate breeze clock.")]
        [SerializeField] private PetalMotionController[] topPetals;
        [Tooltip("Connected spines in visual clockwise order, flattened as Base / Mid / Tip. Source IDs need not be contiguous.")]
        [SerializeField] private Transform[] outerBones;
        [Header("Coordinated lower-layer motion")]
        [SerializeField, Range(0f, 0.72f)] private float liftFollow = 0.72f;
        [SerializeField, Range(1.35f, 1.5f)] private float downwardFollow = 1.35f;
        [SerializeField, Range(0f, 0.5f)] private float swayFollow = 0.5f;

        private Quaternion[] restRotations;
        public Vector3 CurrentLiftDegrees { get; private set; }
        public Vector3 CurrentSwayDegrees { get; private set; }
        public int PetalCount => outerBones == null ? 0 : outerBones.Length / 3;

        private void OnEnable()
        {
            if (topPetals == null || topPetals.Length != 8 || outerBones == null || outerBones.Length < 3 || outerBones.Length % 3 != 0)
            {
                FailSetup();
                return;
            }
            foreach (var top in topPetals)
                if (top == null) { FailSetup(); return; }
            for (int i = 0; i < outerBones.Length; i++)
            {
                if (outerBones[i] == null || (i % 3 != 0 && outerBones[i].parent != outerBones[i - 1]))
                { FailSetup(); return; }
                for (int j = 0; j < i; j++)
                    if (outerBones[j] == outerBones[i]) { FailSetup(); return; }
            }
            if (restRotations == null || restRotations.Length != outerBones.Length)
            {
                restRotations = new Quaternion[outerBones.Length];
                for (int i = 0; i < outerBones.Length; i++) restRotations[i] = outerBones[i].localRotation;
            }
            RestoreRestPose();
        }

        private void FailSetup()
        {
            Debug.LogError("Assign all eight top motion controllers and unique connected three-bone outer spines.", this);
            enabled = false;
        }

        private void LateUpdate() => SynchronizePose();

        /// <summary>Also callable after an explicit source-pose update in validation tools.</summary>
        public void SynchronizePose()
        {
            if (!isActiveAndEnabled || restRotations == null) return;
            Vector3 minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 sway = Vector3.zero;
            foreach (var top in topPetals)
            {
                // A disabled/missing source is treated as resting, never as permission to rise.
                bool active = top != null && top.isActiveAndEnabled;
                Vector3 lift = active ? top.CurrentLiftDegrees : Vector3.zero;
                minimum = Vector3.Min(minimum, lift);
                sway += active ? top.CurrentSwayDegrees : Vector3.zero;
            }
            Vector3 followed = Vector3.zero;
            for (int axis = 0; axis < 3; axis++)
                followed[axis] = minimum[axis] * (minimum[axis] >= 0f ? liftFollow : downwardFollow);
            CurrentLiftDegrees = followed;
            CurrentSwayDegrees = sway * (swayFollow / topPetals.Length);
            for (int i = 0; i < outerBones.Length; i++)
            {
                int joint = i % 3;
                outerBones[i].localRotation = restRotations[i]
                    * Quaternion.AngleAxis(CurrentLiftDegrees[joint], Vector3.right)
                    * Quaternion.AngleAxis(CurrentSwayDegrees[joint], Vector3.forward);
            }
        }

        private void OnDisable() => RestoreRestPose();

        private void RestoreRestPose()
        {
            CurrentLiftDegrees = CurrentSwayDegrees = Vector3.zero;
            if (restRotations == null) return;
            for (int i = 0; outerBones != null && i < Mathf.Min(restRotations.Length, outerBones.Length); i++)
                if (outerBones[i] != null) outerBones[i].localRotation = restRotations[i];
        }
    }
}
