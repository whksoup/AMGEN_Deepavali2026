// Run through Unity MCP eval_file after scripts compile. Keeps the existing top prefab as a base.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Deepavali_PaletteTest.unity") throw new System.InvalidOperationException("Unexpected scene: " + scene.path);
var root = UnityEngine.GameObject.Find("Deepavali Palette Inner Layer");
if (root == null || root.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>(true) != null)
    throw new System.InvalidOperationException("Original flower missing, or outer layer already installed.");
var project = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
var backup = System.IO.Path.Combine(project,"output/flower-petal-prototype/Deepavali_PaletteTest_before_outer.unity");
if (!System.IO.File.Exists(backup)) System.IO.File.Copy(scene.path,backup);
const string assetRoot = "Assets/HK_Signature/Deepavali/PetalPrototype";
const string modelPath = assetRoot + "/Models/Deepavali_OuterPetals.fbx";
const string prefabPath = assetRoot + "/Prefabs/Deepavali Two Layer Flower.prefab";
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath) != null)
    throw new System.InvalidOperationException("Combined prefab already exists.");
var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(modelPath);
importer.importAnimation = false;
importer.importCameras = false;
importer.importLights = false;
importer.optimizeGameObjects = false;
importer.skinWeights = UnityEditor.ModelImporterSkinWeights.Custom;
importer.maxBonesPerVertex = 2;
importer.minBoneWeight = 0.00001f;
importer.isReadable = false;
importer.SaveAndReimport();
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(modelPath);
var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,root.transform);
UnityEditor.Undo.RegisterCreatedObjectUndo(instance,"Add rigged outer petal layer");
instance.name = "Outer Petal Layer - 15";
instance.transform.localPosition = UnityEngine.Vector3.zero;
instance.transform.localRotation = UnityEngine.Quaternion.identity;
instance.transform.localScale = UnityEngine.Vector3.one;
var tops = root.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().OrderBy(p=>p.name).ToArray();
var transforms = instance.GetComponentsInChildren<UnityEngine.Transform>();
var bones = Enumerable.Range(1,15).SelectMany(i=>new[]{"Base","Mid","Tip"}.Select(part=>transforms.Single(t=>t.name=="Outer_"+i.ToString("00")+"_"+part))).ToArray();
var controller = instance.AddComponent<Amgen.Deepavali.OuterPetalLayerMotion>();
var settings = new UnityEditor.SerializedObject(controller);
var topArray = settings.FindProperty("topPetals"); topArray.arraySize=tops.Length;
for(int i=0;i<tops.Length;i++) topArray.GetArrayElementAtIndex(i).objectReferenceValue=tops[i];
var boneArray=settings.FindProperty("outerBones");boneArray.arraySize=bones.Length;
for(int i=0;i<bones.Length;i++)boneArray.GetArrayElementAtIndex(i).objectReferenceValue=bones[i];
settings.ApplyModifiedPropertiesWithoutUndo();
var materials = new System.Collections.Generic.Dictionary<string,UnityEngine.Material>();
foreach(var skin in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
{
    string materialName=skin.sharedMaterial.name;
    if(!materials.TryGetValue(materialName,out var material))
    {
        string path=assetRoot+"/Materials/"+materialName+".mat";
        material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if(material==null)
        {
            material=new UnityEngine.Material(skin.sharedMaterial){name=materialName};
            material.SetFloat("_Cull",(float)UnityEngine.Rendering.CullMode.Off);
            material.SetFloat("_Smoothness",0.3f);
            UnityEditor.AssetDatabase.CreateAsset(material,path);
        }
        materials[materialName]=material;
    }
    skin.sharedMaterial=material;
    skin.quality=UnityEngine.SkinQuality.Bone2;
    skin.updateWhenOffscreen=false;
    // Conservative local bounds include supported upward curls and the downward stress pose.
    skin.localBounds=new UnityEngine.Bounds(new UnityEngine.Vector3(0,.173f,0),new UnityEngine.Vector3(.26f,.41f,.32f));
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(skin);
}
UnityEditor.Undo.RecordObject(root,"Name assembled flower");
root.name="Deepavali Two Layer Flower";
var ground=UnityEngine.GameObject.Find("Ground");
UnityEditor.Undo.RecordObject(ground.transform,"Place backdrop below outer petals");
ground.transform.position=new UnityEngine.Vector3(ground.transform.position.x,-.083f,ground.transform.position.z);
var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(root,prefabPath,UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.Selection.activeGameObject=instance;
return new { scene=scene.path, prefab=prefabPath, outerMeshes=instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Length, bones=bones.Length, topSources=tops.Length, backup, materials=materials.Keys.ToArray() };
