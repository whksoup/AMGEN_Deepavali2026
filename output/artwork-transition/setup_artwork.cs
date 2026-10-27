if (UnityEditor.EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
    throw new System.InvalidOperationException("Setup needs the saved review scene outside Play Mode.");
const string prefabPath = "Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/Deepavali Palette Inner Layer.prefab";
const string scenePath = "Assets/Scenes/Deepavali_PaletteTest.unity";
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != scenePath)
    throw new System.InvalidOperationException("Expected palette review scene.");
Amgen.Deepavali.PetalArtworkController[] Bind(Amgen.Deepavali.FlowerPaletteController flower)
{
    var data = new UnityEditor.SerializedObject(flower);
    var petals = data.FindProperty("petals");
    var controls = new Amgen.Deepavali.PetalArtworkController[petals.arraySize];
    for (int i = 0; i < petals.arraySize; i++)
    {
        var surface = (UnityEngine.Renderer)petals.GetArrayElementAtIndex(i).objectReferenceValue;
        var wrapper = surface.GetComponentInParent<Amgen.Deepavali.PetalMotionController>().gameObject;
        var control = wrapper.GetComponent<Amgen.Deepavali.PetalArtworkController>();
        if (control == null) control = wrapper.AddComponent<Amgen.Deepavali.PetalArtworkController>();
        var binding = new UnityEditor.SerializedObject(control);
        binding.FindProperty("flower").objectReferenceValue = flower;
        binding.FindProperty("surface").objectReferenceValue = surface;
        binding.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.EditorUtility.SetDirty(control);
        controls[i] = control;
    }
    return controls;
}
var contents = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    Bind(contents.GetComponent<Amgen.Deepavali.FlowerPaletteController>());
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(contents); }
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
var controller = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var sceneControls = Bind(controller);
var preview = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerAppearancePreview>();
var previewData = new UnityEditor.SerializedObject(preview);
var list = previewData.FindProperty("artworkControls");
list.arraySize = sceneControls.Length;
for (int i = 0; i < list.arraySize; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sceneControls[i];
previewData.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(preview.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
return new { controls = sceneControls.Length, indices = sceneControls.Select(c => c.PetalIndex).ToArray(), sceneSaved = !preview.gameObject.scene.isDirty };
