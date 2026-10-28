if(Application.isPlaying)throw new System.Exception("Exit Play Mode first");
var offsets=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("output/refined-flower-integration/outer-mount-offsets.json"));
if(!(bool)offsets["success"])throw new System.Exception("No validated mounting offsets");
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HK_Signature/Deepavali/PetalPrototype/Models/Refined Flower/Deepavali_RefinedFlower.fbx");
var sourceSkins=source.GetComponentsInChildren<SkinnedMeshRenderer>().ToDictionary(r=>r.name);
void Apply(Amgen.Deepavali.FlowerPaletteController flower)
{
    foreach(var skin in flower.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.name.StartsWith("OuterPetal_")))
    {
        float offset=(float)offsets["offsets_flower_m"][skin.name];var src=sourceSkins[skin.name];
        var bone=skin.bones.Single(b=>b.name.EndsWith("_Base"));var sourceBone=src.bones.Single(b=>b.name.EndsWith("_Base"));
        skin.transform.localPosition=skin.transform.parent.InverseTransformPoint(flower.transform.TransformPoint(src.transform.position+Vector3.up*offset));
        bone.localPosition=bone.parent.InverseTransformPoint(flower.transform.TransformPoint(sourceBone.position+Vector3.up*offset));
        EditorUtility.SetDirty(skin.transform);EditorUtility.SetDirty(bone);
    }
}
string active=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
foreach(string name in new[]{"Deepavali_PetalWheelSequence","Deepavali_LightSequence","Deepavali_PetalLightingPrototype","Deepavali_PaletteTest"})
{var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");Apply(UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>(FindObjectsInactive.Include));UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}
foreach(string name in new[]{"Deepavali Peacock Flower","Deepavali Two Layer Flower"})
{string path="Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/"+name+".prefab";var contents=PrefabUtility.LoadPrefabContents(path);try{Apply(contents.GetComponent<Amgen.Deepavali.FlowerPaletteController>());PrefabUtility.SaveAsPrefabAsset(contents,path);}finally{PrefabUtility.UnloadPrefabContents(contents);}}
AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.OpenScene(active);return "Saved fixed outer mounting depths (0 to 1.855 mm below the sculpt) in four scenes and two prefabs";
