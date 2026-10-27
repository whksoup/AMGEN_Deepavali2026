// Run once through Unity MCP eval_file. The single-petal source scene is preserved.
var assetRoot = "Assets/HK_Signature/Deepavali/PetalPrototype";
var scenePath = "Assets/Scenes/Deepavali_InnerLayerTest.unity";
var petalPath = assetRoot + "/Prefabs/Deepavali Inner Petal.prefab";
var layerPath = assetRoot + "/Prefabs/Deepavali Inner Layer.prefab";
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
        throw new System.InvalidOperationException("Preserve unsaved scene edits first.");
foreach (var path in new[] { scenePath, petalPath, layerPath })
    if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null)
        throw new System.InvalidOperationException("Refusing to overwrite existing asset: " + path);
if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Deepavali_PetalTest.unity", scenePath))
    throw new System.InvalidOperationException("Could not copy source scene.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
var oldPetal = UnityEngine.GameObject.Find("Deepavali Single Petal");
var layer = new UnityEngine.GameObject("Deepavali Inner Layer");
var sourceDisc = oldPetal.GetComponentsInChildren<UnityEngine.Transform>(true).Single(t => t.name == "CenterDisc");
var disc = UnityEngine.Object.Instantiate(sourceDisc.gameObject);
disc.name = "Center Disc";
disc.transform.SetParent(layer.transform, true);
disc.transform.SetPositionAndRotation(sourceDisc.position, sourceDisc.rotation);
disc.transform.localScale = sourceDisc.lossyScale;
var discCollider = disc.AddComponent<UnityEngine.MeshCollider>();
discCollider.sharedMesh = disc.GetComponent<UnityEngine.MeshFilter>().sharedMesh;

UnityEditor.PrefabUtility.UnpackPrefabInstance(oldPetal, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
UnityEngine.Object.DestroyImmediate(oldPetal.GetComponentsInChildren<UnityEngine.Transform>(true).Single(t => t.name == "CenterDisc").gameObject);
oldPetal.name = "Deepavali Inner Petal";
var pick = oldPetal.AddComponent<Amgen.Deepavali.PetalPickTarget>();
var pickData = new UnityEditor.SerializedObject(pick);
pickData.FindProperty("motion").objectReferenceValue = oldPetal.GetComponent<Amgen.Deepavali.PetalMotionController>();
pickData.FindProperty("surface").objectReferenceValue = oldPetal.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
pickData.ApplyModifiedPropertiesWithoutUndo();
var petalAsset = UnityEditor.PrefabUtility.SaveAsPrefabAsset(oldPetal, petalPath);
UnityEngine.Object.DestroyImmediate(oldPetal);
UnityEngine.Object.DestroyImmediate(UnityEngine.GameObject.Find("Petal Test Input - Tap Click Space"));

var targets = new Amgen.Deepavali.PetalPickTarget[8];
for (int i = 0; i < 8; i++)
{
    var petal = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(petalAsset, layer.transform);
    petal.name = "Inner Petal " + (i + 1).ToString("00");
    petal.transform.localPosition = UnityEngine.Vector3.zero;
    petal.transform.localRotation = UnityEngine.Quaternion.Euler(0f, i * 45f, 0f);
    var motion = petal.GetComponent<Amgen.Deepavali.PetalMotionController>();
    var motionData = new UnityEditor.SerializedObject(motion);
    motionData.FindProperty("phaseOffset").floatValue = i * 0.35f;
    motionData.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(motion);
    targets[i] = petal.GetComponent<Amgen.Deepavali.PetalPickTarget>();
}
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(layer, layerPath, UnityEditor.InteractionMode.AutomatedAction);

var camera = UnityEngine.Camera.main;
camera.transform.position = new UnityEngine.Vector3(0f, 1.5f, 0f);
camera.farClipPlane = 3f;
var ground = UnityEngine.GameObject.Find("Ground").transform;
ground.position = new UnityEngine.Vector3(0f, -0.008f, 0f);
ground.localScale = UnityEngine.Vector3.one * 0.3f;
var label = UnityEngine.GameObject.Find("Test Instructions").GetComponentInChildren<UnityEngine.UI.Text>();
label.text = "INNER PETAL LAYER\nTap or click a petal to pulse it";
var input = new UnityEngine.GameObject("Inner Layer Input - Click or Tap a Petal").AddComponent<Amgen.Deepavali.PetalLayerPointerInput>();
var inputData = new UnityEditor.SerializedObject(input);
inputData.FindProperty("interactionCamera").objectReferenceValue = camera;
var petalsProperty = inputData.FindProperty("petals");
petalsProperty.arraySize = targets.Length;
for (int i = 0; i < targets.Length; i++) petalsProperty.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
var occludersProperty = inputData.FindProperty("occluders");
occludersProperty.arraySize = 1;
occludersProperty.GetArrayElementAtIndex(0).objectReferenceValue = discCollider;
inputData.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.Selection.activeGameObject = layer;
if (UnityEditor.SceneView.lastActiveSceneView != null)
    UnityEditor.SceneView.lastActiveSceneView.LookAt(UnityEngine.Vector3.zero, camera.transform.rotation, 0.6f);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
return new { scene = scenePath, prefab = layerPath, petals = targets.Length, bones = targets.Sum(t => t.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().bones.Length),
    triangles = targets.Sum(t => (int)t.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().sharedMesh.GetIndexCount(0) / 3) + (int)discCollider.sharedMesh.GetIndexCount(0) / 3,
    phases = targets.Select(t => new UnityEditor.SerializedObject(t.Motion).FindProperty("phaseOffset").floatValue).ToArray() };
