if(Application.isPlaying)throw new System.Exception("Exit Play Mode first");
void Configure(Amgen.Deepavali.FlowerPaletteController flower)
{
    foreach(var motion in flower.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>(true))
    {var data=new SerializedObject(motion);data.FindProperty("pulseLift").vector3Value=new Vector3(.08f,.35f,8f);data.ApplyModifiedPropertiesWithoutUndo();}
    foreach(var skin in flower.GetComponentsInChildren<SkinnedMeshRenderer>(true))
    {skin.updateWhenOffscreen=false;var bounds=skin.sharedMesh.bounds;bounds.Expand(.3f);skin.localBounds=bounds;EditorUtility.SetDirty(skin);}
    foreach(var collider in flower.GetComponentsInChildren<MeshCollider>(true))
    {collider.sharedMesh=collider.GetComponent<MeshFilter>().sharedMesh;EditorUtility.SetDirty(collider);}
}
string active=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
foreach(string name in new[]{"Deepavali_PetalWheelSequence","Deepavali_LightSequence","Deepavali_PetalLightingPrototype","Deepavali_PaletteTest"})
{var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");Configure(UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>(FindObjectsInactive.Include));UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}
foreach(string name in new[]{"Deepavali Peacock Flower","Deepavali Two Layer Flower"})
{string path="Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/"+name+".prefab";var contents=PrefabUtility.LoadPrefabContents(path);try{Configure(contents.GetComponent<Amgen.Deepavali.FlowerPaletteController>());PrefabUtility.SaveAsPrefabAsset(contents,path);}finally{PrefabUtility.UnloadPrefabContents(contents);}}
AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.OpenScene(active);return "Saved motion, bounds and center picking collider in four scenes and two prefabs";
