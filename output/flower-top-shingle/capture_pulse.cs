// Render a simultaneous, alternating full pulse without retaining a pose change.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var tops = UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalMotionController>(UnityEngine.FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
var outer = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.OuterPetalLayerMotion>();
var type = typeof(Amgen.Deepavali.PetalMotionController);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
string[] fields = { "pulseWeight", "idleFade", "currentLiftDegrees", "currentSwayDegrees" };
var saved = tops.Select(p => fields.Select(f => type.GetField(f, flags).GetValue(p)).ToArray()).ToArray();
var bones = tops.SelectMany(p => p.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().bones).ToArray();
var rotations = bones.Select(b => b.localRotation).ToArray();
var camera = UnityEngine.Camera.main;
var oldTarget = camera.targetTexture;
var oldActive = UnityEngine.RenderTexture.active;
var target = UnityEngine.RenderTexture.GetTemporary(1600, 1118, 24, UnityEngine.RenderTextureFormat.ARGB32);
var texture = new UnityEngine.Texture2D(1600, 1118, UnityEngine.TextureFormat.RGB24, false);
try
{
    string[] boneFields = { "baseBone", "middleBone", "tipBone" }, restFields = { "baseRest", "middleRest", "tipRest" };
    for (int i = 0; i < tops.Length; i++)
    {
        var p = tops[i];
        type.GetField("pulseWeight", flags).SetValue(p, i % 2 == 0 ? 1f : 0f);
        type.GetField("idleFade", flags).SetValue(p, 1f);
        for (int k = 0; k < 3; k++) type.GetMethod("ApplyBone", flags).Invoke(p, new object[] {
            type.GetField(boneFields[k], flags).GetValue(p), type.GetField(restFields[k], flags).GetValue(p), k });
    }
    outer.SynchronizePose();
    camera.targetTexture = target;
    camera.Render();
    UnityEngine.RenderTexture.active = target;
    texture.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 1118), 0, 0);
    texture.Apply();
    System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath("output/flower-top-shingle/after-alternating-pulse.png"), texture.EncodeToPNG());
}
finally
{
    camera.targetTexture = oldTarget;
    UnityEngine.RenderTexture.active = oldActive;
    UnityEngine.RenderTexture.ReleaseTemporary(target);
    UnityEngine.Object.Destroy(texture);
    for (int i = 0; i < tops.Length; i++) for (int k = 0; k < fields.Length; k++) type.GetField(fields[k], flags).SetValue(tops[i], saved[i][k]);
    for (int i = 0; i < bones.Length; i++) bones[i].localRotation = rotations[i];
    outer.SynchronizePose();
}
return "output/flower-top-shingle/after-alternating-pulse.png";
