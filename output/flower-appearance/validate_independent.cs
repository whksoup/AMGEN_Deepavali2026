if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Use Edit Mode for this isolated prefab check.");
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/Deepavali Palette Inner Layer.prefab");
var instance = UnityEngine.Object.Instantiate(prefab);
try
{
    var c = instance.GetComponent<Amgen.Deepavali.FlowerPaletteController>();
    // MonoBehaviour callbacks are not automatic in Edit Mode.
    c.SendMessage("OnEnable");
    var petals = instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().OrderBy(s => s.transform.root.name).ToArray();
    var refs = new UnityEditor.SerializedObject(c).FindProperty("petals");
    bool independent = true, tied = true, sharedTexture = true;
    var ids = new System.Collections.Generic.HashSet<int>();
    UnityEngine.Texture mask = null;
    for (int i = 0; i < c.PetalCount; i++)
    {
        var state = (Amgen.Deepavali.FlowerPaletteState)(i % 7);
        independent &= c.SetPetalState(i, state) && c.SetPetalEmissionScale(i, i * 0.25f);
        c.PaletteSet.TryGet(state, out var p);
        var renderer = (UnityEngine.Renderer)refs.GetArrayElementAtIndex(i).objectReferenceValue;
        var mat = renderer.sharedMaterial;
        ids.Add(mat.GetInstanceID());
        if (i == 0) mask = mat.GetTexture("_BaseMap");
        sharedTexture &= mat.GetTexture("_BaseMap") == mask;
        independent &= c.GetPetalState(i) == state && mat.GetColor("_BaseColor") == p.fill.linear;
        tied &= mat.GetColor("_BaseColor") == p.fill.linear && mat.GetFloat("_EdgeIntensity") == p.edgeIntensity && mat.GetFloat("_EmissionScale") == i * 0.25f;
    }
    bool rejects = !c.SetPetalState(-1, Amgen.Deepavali.FlowerPaletteState.Base) && !c.SetPetalState(0, (Amgen.Deepavali.FlowerPaletteState)9) && !c.SetPetalEmissionScale(0, float.NaN);
    c.SendMessage("OnDisable");
    c.SendMessage("OnEnable");
    bool retained = c.GetPetalState(5) == Amgen.Deepavali.FlowerPaletteState.Blue;
    c.SendMessage("OnDisable");
    bool restored = true;
    for (int i = 0; i < refs.arraySize; i++) restored &= ((UnityEngine.Renderer)refs.GetArrayElementAtIndex(i).objectReferenceValue).sharedMaterial == UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Petal Palette Lit.mat");
    var report = new { passed = independent && tied && sharedTexture && ids.Count == 8 && rejects && retained && restored, independent, tied, sharedTexture, materials = ids.Count, rejects, retained, restored };
    string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText("output/flower-appearance/independent-validation.json", json);
    return report;
}
finally { UnityEngine.Object.DestroyImmediate(instance); }
