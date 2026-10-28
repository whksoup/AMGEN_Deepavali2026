const string root="Assets/HK_Signature/Deepavali/PetalPrototype";
var model=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Models/Refined Flower/Deepavali_RefinedFlower.fbx");
var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
var checks=new Dictionary<string,bool>();
checks["allEightInnerAnd14NoncontiguousOuterSourceIDs"]=skins.Count(r=>r.name=="InnerPetal_01"||r.name.StartsWith("REF_TopPetal_"))==8&&skins.Count(r=>r.name.StartsWith("OuterPetal_"))==14&&!skins.Any(r=>r.name=="OuterPetal_02"||r.name=="OuterPetal_16");
checks["noPreviewObjectsOrAnimation"]=model.GetComponentsInChildren<Camera>().Length==0&&model.GetComponentsInChildren<Light>().Length==0&&model.GetComponentsInChildren<Renderer>().Length==23&&!AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).Any(a=>a is AnimationClip);
checks["threeBonesTwoWeightsNormalized"]=skins.All(s=>s.bones.Length==3&&s.sharedMesh.bindposes.Length==3&&s.sharedMesh.boneWeights.All(w=>w.weight2==0&&w.weight3==0&&Mathf.Abs(w.weight0+w.weight1-1)<.000001f));
checks["bothUVChannelsImported"]=skins.All(s=>s.sharedMesh.uv.Length==s.sharedMesh.vertexCount&&s.sharedMesh.uv2.Length==s.sharedMesh.vertexCount);
foreach(string name in new[]{"Deepavali Peacock Flower","Deepavali Two Layer Flower"})
{
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/"+name+".prefab");var f=prefab.GetComponent<Amgen.Deepavali.FlowerPaletteController>();var data=new SerializedObject(f);var apps=data.FindProperty("petalAppearances");
 checks[name+"_22Renderers66Bones"]=f.PetalCount==22&&prefab.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(s=>s.bones).Distinct().Count()==66&&prefab.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>().PetalCount==14;
 checks[name+"_completeArtworkAndLightingMaps"]=Enumerable.Range(0,22).All(i=>{var p=apps.GetArrayElementAtIndex(i);var mat=(Material)p.FindPropertyRelative("materialOverride").objectReferenceValue;var tex=(Texture2D)p.FindPropertyRelative("spatialLightingMap").objectReferenceValue;return mat!=null&&mat.GetFloat("_ArtworkFitEnabled")==1&&mat.GetTexture("_ArtworkFitMap")!=null&&tex!=null&&!((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex))).sRGBTexture;});
 checks[name+"_conservativeBounds"]=prefab.GetComponentsInChildren<SkinnedMeshRenderer>().All(s=>!s.updateWhenOffscreen&&s.localBounds.size.z>.29f);
}
checks["shaderHasNoErrors"]=ShaderUtil.GetShaderMessages(Shader.Find("Amgen/Deepavali/Petal Palette Lit")).All(m=>m.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error);
var report=new{passed=checks.Values.All(v=>v),checks,petalTriangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3)};
var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/inner-petal-regularized/asset-validation.json",json);return json;
