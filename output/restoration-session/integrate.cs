if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/Deepavali_PetalWheelSequence.unity" || scene.isDirty)
    throw new System.Exception("Expected the saved main wheel scene.");
if (UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>() != null)
    throw new System.Exception("Already integrated; edit the existing session.");
var root = new GameObject("Deepavali Session");
UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Integrate restoration session");
var r = root.AddComponent<Amgen.Deepavali.ScreenRestorationController>();
r.interactionCamera = Camera.main;
r.compositeShader = Shader.Find("Amgen/Deepavali/Screen Restoration");
r.autoComplete = false; r.paintingEnabled = false; r.completionCoverage = .8f;
var bs = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
r.idleBehaviours = bs.Where(b => b is Amgen.Deepavali.PetalMotionController || b is Amgen.Deepavali.OuterPetalLayerMotion ||
    b is Amgen.Deepavali.PetalRingPulse || b is Amgen.Deepavali.PetalWheelLightSequence || b is Amgen.Deepavali.PetalLayerPointerInput).Cast<Behaviour>().ToArray();
foreach (var b in bs.Where(b => b is Amgen.Deepavali.FlowerAppearancePreview || b is Amgen.Deepavali.FlowerSequencePreview || b is Amgen.Deepavali.PetalWheelSequencePreview))
{ UnityEditor.Undo.RecordObject(b, "Disable review controls in experience"); b.enabled = false; UnityEditor.EditorUtility.SetDirty(b); }
var lamps = root.AddComponent<Amgen.Deepavali.RestorationLampResponse>();
lamps.restoration = r; lamps.background = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var s = root.AddComponent<Amgen.Deepavali.DeepavaliSessionController>();
s.restoration = r; s.lamps = lamps;
s.flower = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
s.sequence = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalWheelLightSequence>();
UnityEditor.Undo.RecordObject(s.sequence, "Session owns wheel start");
s.sequence.autoPreview = false;
s.handPrompt = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.HandHereInteraction>(UnityEngine.FindObjectsInactive.Include);
UnityEditor.Undo.RecordObject(s.handPrompt, "Use hover prompt");
s.handPrompt.externallyDriven = true; s.handPrompt.gameObject.SetActive(true);
var canvas = s.handPrompt.GetComponentInParent<Amgen.Deepavali.HandGuidanceCanvas>(true);
UnityEditor.Undo.RecordObject(canvas, "Keep prompt above darkness");
canvas.gameObject.SetActive(true); canvas.renderAfterPostProcessing = true;
var uiCanvas = canvas.GetComponent<Canvas>();
UnityEditor.Undo.RecordObject(uiCanvas, "Render prompt above darkness");
uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(uiCanvas);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(s.handPrompt);
var lightObject = new GameObject("Restoration Center Light");
lightObject.transform.SetParent(root.transform);
lightObject.transform.position = s.flower.transform.position + Vector3.up * .28f;
s.centerLight = lightObject.AddComponent<Light>();
s.centerLight.type = LightType.Point; s.centerLight.color = new Color(1, .68f, .25f);
s.centerLight.range = 1.15f; s.centerLight.intensity = 0; s.centerLight.enabled = false;
s.centerLight.shadows = LightShadows.None;
UnityEditor.EditorUtility.SetDirty(s.sequence);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.Selection.activeGameObject = root;
var result = new { scene = scene.path, idleBehaviours = r.idleBehaviours.Select(b => new { type = b.GetType().Name, enabled = b.enabled }).ToArray(), lamps = lamps.background.lamps.Length, s.requiredCoverage, s.coloredHoldDuration, s.openingDwellDuration, s.spinDwellDuration };
System.IO.File.WriteAllText("output/restoration-session/scene-setup.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
