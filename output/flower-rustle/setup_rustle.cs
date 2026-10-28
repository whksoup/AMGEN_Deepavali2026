if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
const string root = "Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/";
var petal = UnityEditor.PrefabUtility.LoadPrefabContents(root + "Deepavali Inner Petal.prefab");
try
{
    var motion = new UnityEditor.SerializedObject(petal.GetComponent<Amgen.Deepavali.PetalMotionController>());
    motion.FindProperty("pulseLift").vector3Value = new UnityEngine.Vector3(5f, 9f, 8f);
    motion.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(petal, root + "Deepavali Inner Petal.prefab");
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(petal); }
foreach (string name in new[] { "Deepavali Inner Layer", "Deepavali Textured Inner Layer", "Deepavali Palette Inner Layer" })
{
    string path = root + name + ".prefab";
    string backup = System.IO.Path.GetFullPath("output/flower-rustle/before-" + name + ".prefab");
    if (!System.IO.File.Exists(backup)) System.IO.File.Copy(path, backup);
    var layer = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        var petals = layer.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().OrderBy(p => p.name).ToArray();
        if (petals.Length != 8) throw new System.InvalidOperationException("Expected eight upper petals.");
        var ring = layer.GetComponent<Amgen.Deepavali.PetalRingPulse>();
        if (ring == null) ring = layer.AddComponent<Amgen.Deepavali.PetalRingPulse>();
        var serialized = new UnityEditor.SerializedObject(ring);
        var array = serialized.FindProperty("clockwisePetals");
        array.arraySize = petals.Length;
        for (int i = 0; i < petals.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = petals[i];
        serialized.FindProperty("firstClockwise").floatValue = .55f;
        serialized.FindProperty("secondClockwise").floatValue = .22f;
        serialized.FindProperty("anticlockwiseRustle").floatValue = .12f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        ring.enabled = true;
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(layer, path);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(layer); }
}
UnityEditor.AssetDatabase.SaveAssets();
return "Restored the strong tap bend and wired 55% / 22% clockwise, 12% anticlockwise follow-through.";
