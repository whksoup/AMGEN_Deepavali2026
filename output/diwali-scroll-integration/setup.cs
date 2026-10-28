if (Application.isPlaying) throw new System.Exception("Setup requires Edit Mode.");
const string root = "Assets/HK_Signature/Deepavali/PetalPrototype";
const string folder = root + "/Symbols/Outcome Icons";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder(root + "/Symbols", "Outcome Icons");
var session = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(session == null || session.flower == null || session.sequence == null) throw new System.Exception("Open the integrated main scene.");
string[] labels = { "Lotus", "Sunburst", "Star", "Sprout", "Diya", "People and Heart", "Gift Box", "Stacked Coins", "Center Mandala" };
var source = ScriptableObject.CreateInstance<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>();
source.outputName = "Outcome Icons";
source.tiles = labels.Select((label,i) => new Amgen.Deepavali.Editor.SymbolMaskSource { label=label,
    mask=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(root + "/Symbols/Initial State/Icons/" + i.ToString("00") + "-" + label + ".png") }).ToArray();
UnityEditor.AssetDatabase.CreateAsset(source, folder + "/Outcome Icon Source.asset");
var icons = Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.Build(source);
var outcomes = ScriptableObject.CreateInstance<Amgen.Deepavali.DeepavaliOutcomeSet>();
outcomes.icons = icons; outcomes.openingCenterIcon = 8;
var palettes = new[] { Amgen.Deepavali.FlowerPaletteState.Red, Amgen.Deepavali.FlowerPaletteState.Orange,
    Amgen.Deepavali.FlowerPaletteState.Blue, Amgen.Deepavali.FlowerPaletteState.Green, Amgen.Deepavali.FlowerPaletteState.Pink,
    Amgen.Deepavali.FlowerPaletteState.Cyan, Amgen.Deepavali.FlowerPaletteState.Gold, Amgen.Deepavali.FlowerPaletteState.Purple };
int[] iconIds = {3,4,2,7,5,6,1,0};
outcomes.entries = palettes.Select((palette,i)=>new Amgen.Deepavali.DeepavaliOutcome { palette=palette, symbol=labels[iconIds[i]],
    openingSymbol=i, icon=iconIds[i], scrollVariant=palette==Amgen.Deepavali.FlowerPaletteState.Gold ? "Yellow" : palette.ToString() }).ToArray();
if(!outcomes.Validate(out var error)) throw new System.Exception(error);
UnityEditor.AssetDatabase.CreateAsset(outcomes, root + "/Rendering/Light Sequence/Deepavali Outcomes.asset");
UnityEditor.Undo.RecordObject(session.flower,"Assign shared outcome mapping");
session.flower.outcomeSet = outcomes; UnityEditor.EditorUtility.SetDirty(session.flower);

var canvasGO = new GameObject("Diwali Scroll Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
var scaler=canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution=new Vector2(2560,1118);scaler.matchWidthOrHeight=.5f;
var go = new GameObject("Diwali Scroll",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
go.transform.SetParent(canvasGO.transform,false);
var rect = (RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
var overlay=go.AddComponent<DiwaliScrollOverlay>();
string[] variants={"Orange","Blue","Green","Red","Yellow","Purple","Pink","Cyan"};
UnityEngine.Video.VideoClip Clip(string name,string phase) => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/DiwaliScroll/Videos/diwali_"+name.ToLowerInvariant()+"_"+phase+".webm");
overlay.variants=variants.Select(name=>new DiwaliScrollOverlay.Variant{name=name,spawnClip=Clip(name,"spawn"),idleClip=Clip(name,"idle"),hideClip=Clip(name,"hide")}).ToArray();
overlay.toggleOnMouseClick=overlay.showOnStart=overlay.waitForIdleLoopEnd=false;
overlay.GetComponent<UnityEngine.UI.RawImage>().raycastTarget=false;
overlay.GetComponent<UnityEngine.UI.RawImage>().enabled=false;
if(!overlay.ValidateVariants(out error)) throw new System.Exception(error);
if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/DiwaliScroll/Prefabs")) UnityEditor.AssetDatabase.CreateFolder("Assets/DiwaliScroll","Prefabs");
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGO,"Assets/DiwaliScroll/Prefabs/Diwali Scroll Canvas.prefab",UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.Undo.RecordObject(session,"Connect result scroll and settings");
session.scrollOverlay=overlay;
session.scrollDelayAfterResultSeconds=2;session.scrollHoldSeconds=15;
var settings=ScriptableObject.CreateInstance<Amgen.Deepavali.DeepavaliSessionSettings>();settings.CaptureFrom(session);
UnityEditor.AssetDatabase.CreateAsset(settings,"Assets/HK_Signature/Deepavali/Restoration/Deepavali Session Settings.asset");
session.settingsPreset=settings;UnityEditor.EditorUtility.SetDirty(session);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(session.gameObject.scene);
var report=new{scene=session.gameObject.scene.path,outcomes=outcomes.entries,icons=icons.Count,variants=overlay.variants.Select(v=>new{v.name,spawn=v.spawnClip.length,idle=v.idleClip.length,hide=v.hideClip.length,width=v.spawnClip.width,height=v.spawnClip.height,fps=v.spawnClip.frameRate}).ToArray(),settings=UnityEditor.AssetDatabase.GetAssetPath(settings)};
System.IO.File.WriteAllText("output/diwali-scroll-integration/scene-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
