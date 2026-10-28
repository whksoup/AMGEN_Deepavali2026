if(Application.isPlaying)throw new System.Exception("Edit Mode required");
const string root="Assets/HK_Signature/Deepavali/PetalPrototype";
const string work="output/inner-petal-regularized";
const string art=root+"/Symbols/Rootward Inner";
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(work+"/staged/source-manifest.json"));
var layout=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(work+"/staged/layout.json"));
string current=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.Exception("Unsaved scene; do not overwrite");
AssetDatabase.StartAssetEditing();
try
{
 System.IO.File.Copy(work+"/staged/Deepavali_RefinedFlower.fbx",root+"/Models/Refined Flower/Deepavali_RefinedFlower.fbx",true);
 foreach(var p in System.IO.Directory.GetFiles(work+"/staged/Decorated Masks","*.png"))System.IO.File.Copy(p,art+"/Decorated Masks/"+System.IO.Path.GetFileName(p),true);
 foreach(var p in System.IO.Directory.GetFiles(work+"/staged/Refitted Maps","*.png"))System.IO.File.Copy(p,root+"/Textures/Refined Flower/"+System.IO.Path.GetFileName(p),true);
}
finally{AssetDatabase.StopAssetEditing();}
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var model=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Models/Refined Flower/Deepavali_RefinedFlower.fbx");
var sourceSkins=model.GetComponentsInChildren<SkinnedMeshRenderer>().ToDictionary(r=>r.name);
var source=AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>(art+"/Rootward Inner Source.asset");
var set=Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.Build(source);
var contexts=new List<object>();
void Configure(Amgen.Deepavali.FlowerPaletteController flower,string context)
{
 if(flower.PetalCount!=22)throw new System.Exception("Expected integrated 22-petal flower: "+context);
 var data=new SerializedObject(flower);var apps=data.FindProperty("petalAppearances");
 for(int i=0;i<8;i++)
 {
  var skin=(SkinnedMeshRenderer)flower.GetPetalRenderer(i);string name=(string)manifest["inner"][i];
  if(skin.name!=name)throw new System.Exception("Index mismatch: "+context+" "+i);
  var dims=manifest["meshes"][i];var src=sourceSkins[name];
  skin.sharedMesh=src.sharedMesh;
  // Same names, topology, weights, transforms and bind poses; retain existing bones.
  skin.updateWhenOffscreen=false;var bounds=src.sharedMesh.bounds;bounds.Expand(.3f);skin.localBounds=bounds;
  var app=apps.GetArrayElementAtIndex(i);
  app.FindPropertyRelative("artworkMin").vector2Value=new Vector2((float)dims["min"][0],0);
  app.FindPropertyRelative("artworkSize").vector2Value=new Vector2((float)layout[i]["width"],(float)layout[i]["length"]);
  var material=(Material)app.FindPropertyRelative("materialOverride").objectReferenceValue;
  if(material==null)throw new System.Exception("Missing per-petal material");
  material.SetFloat("_StartupMeshUV",1);
  var r=layout[i]["icon_region"].Values<float>().ToArray();
  material.SetVector("_PetalLightIconRegion",new Vector4(r[0],r[1],r[2],r[3]));
  EditorUtility.SetDirty(material);EditorUtility.SetDirty(skin);
  if(PrefabUtility.IsPartOfPrefabInstance(skin))PrefabUtility.RecordPrefabInstancePropertyModifications(skin);
 }
 data.FindProperty("startupSymbolSet").objectReferenceValue=set;
 data.ApplyModifiedPropertiesWithoutUndo();
 foreach(var motion in flower.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>(true))
 {
  var m=new SerializedObject(motion);
  m.FindProperty("liftBias").vector3Value=new Vector3(.12f,.35f,1f);
  m.FindProperty("idleLift").vector3Value=new Vector3(.035f,.12f,.65f);
  m.FindProperty("idleSway").vector3Value=new Vector3(.01f,.06f,.4f);
  m.FindProperty("pulseLift").vector3Value=new Vector3(.035f,.16f,5f);
  m.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(motion);
  if(PrefabUtility.IsPartOfPrefabInstance(motion))PrefabUtility.RecordPrefabInstancePropertyModifications(motion);
 }
 EditorUtility.SetDirty(flower);
 if(PrefabUtility.IsPartOfPrefabInstance(flower))PrefabUtility.RecordPrefabInstancePropertyModifications(flower);
 contexts.Add(new{context,inner=8,outer=14});
}
foreach(string n in new[]{"Deepavali Peacock Flower","Deepavali Two Layer Flower"})
{
 string path=root+"/Prefabs/"+n+".prefab";var obj=PrefabUtility.LoadPrefabContents(path);
 try{Configure(obj.GetComponent<Amgen.Deepavali.FlowerPaletteController>(),path);PrefabUtility.SaveAsPrefabAsset(obj,path);}
 finally{PrefabUtility.UnloadPrefabContents(obj);}
}
foreach(string n in new[]{"Deepavali_PetalWheelSequence","Deepavali_LightSequence","Deepavali_PetalLightingPrototype","Deepavali_PaletteTest","Deepavali_RestorationTest"})
{
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/"+n+".unity");
 Configure(UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>(FindObjectsInactive.Include),scene.path);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.OpenScene(current);
System.IO.File.WriteAllText(work+"/integration.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{contexts,atlas=AssetDatabase.GetAssetPath(set.Atlas),atlasSize=set.Atlas.width+"x"+set.Atlas.height},Newtonsoft.Json.Formatting.Indented));
return new{updatedContexts=contexts.Count,inner=8,outer=14,atlas=set.Count};
