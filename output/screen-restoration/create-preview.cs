// Execute with the Unity MCP eval_file tool in Edit Mode. Creates a separate scene once.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
const string sourcePath = "Assets/Scenes/Deepavali_PetalWheelSequence.unity";
const string destinationPath = "Assets/Scenes/Deepavali_RestorationTest.unity";
if (System.IO.File.Exists(destinationPath)) throw new System.InvalidOperationException("The restoration preview already exists; edit it in place.");
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (active.isDirty) throw new System.InvalidOperationException("Save the current scene before creating the preview.");
var sourceHash = System.Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.IO.File.ReadAllBytes(sourcePath)));
if (!UnityEditor.AssetDatabase.CopyAsset(sourcePath, destinationPath)) throw new System.Exception("Scene copy failed.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(destinationPath);
foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (b is Amgen.Deepavali.FlowerAppearancePreview || b is Amgen.Deepavali.FlowerSequencePreview || b is Amgen.Deepavali.PetalWheelSequencePreview)
        b.enabled = false;
}
var guide = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.HandGuidanceCanvas>();
if (guide != null) guide.gameObject.SetActive(false);
var camera = UnityEngine.Camera.main;
camera.rect = new UnityEngine.Rect(0, 0, 1, 1);
var root = new UnityEngine.GameObject("Pre-idle Screen Restoration");
var controller = root.AddComponent<Amgen.Deepavali.ScreenRestorationController>();
controller.interactionCamera = camera;
controller.compositeShader = UnityEngine.Shader.Find("Amgen/Deepavali/Screen Restoration");
var sequence = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalWheelLightSequence>();
sequence.autoPreview = false;
var behaviours = UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
controller.idleBehaviours = behaviours.Where(b => b is Amgen.Deepavali.PetalMotionController || b is Amgen.Deepavali.OuterPetalLayerMotion ||
    b is Amgen.Deepavali.PetalRingPulse || b is Amgen.Deepavali.PetalWheelLightSequence || b is Amgen.Deepavali.PetalLayerPointerInput).Cast<UnityEngine.Behaviour>().ToArray();
var lamps = root.AddComponent<Amgen.Deepavali.RestorationLampResponse>();
lamps.restoration = controller; lamps.background = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var preview = root.AddComponent<Amgen.Deepavali.RestorationPreview>(); preview.restoration = controller; preview.sequence = sequence;
var flower = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
UnityEditor.Events.UnityEventTools.AddPersistentListener(controller.onRestorationStarted, flower.ResetToInitialState);
UnityEditor.Events.UnityEventTools.AddPersistentListener(controller.onRestored, sequence.ResetPreview);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.Selection.activeGameObject = root;
var afterHash = System.Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.IO.File.ReadAllBytes(sourcePath)));
var result = new { scene = destinationPath, sourceUnchanged = sourceHash == afterHash, sourceHash, idleComponents = controller.idleBehaviours.Length, lamps = lamps.background.lamps.Length };
System.IO.File.WriteAllText("output/screen-restoration/scene-setup.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
