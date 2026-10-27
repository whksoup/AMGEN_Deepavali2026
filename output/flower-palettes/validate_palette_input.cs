if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
var controller=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var input=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var bar=UnityEngine.GameObject.Find("Palette Choices").transform;
UnityEngine.Canvas.ForceUpdateCanvases();
var points=Enumerable.Range(0,7).Select(i=>UnityEngine.RectTransformUtility.WorldToScreenPoint(null,((UnityEngine.RectTransform)bar.GetChild(i)).TransformPoint(((UnityEngine.RectTransform)bar.GetChild(i)).rect.center))).ToArray();
var motions=controller.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().OrderBy(m=>m.name).ToArray();
var skins=motions.Select(m=>m.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>()).ToArray();
foreach(var m in motions) {m.enabled=false;m.enabled=true;}
var originalPetal=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Petal Palette Lit.mat");
var originalCenter=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Center Palette.mat");
var disc=UnityEngine.GameObject.Find("Center Disc").GetComponent<UnityEngine.Renderer>();
var rootPositions=skins.Select(s=>s.bones[0].position).ToArray();
var originalMouse=UnityEngine.InputSystem.Mouse.current;
var originalTouch=UnityEngine.InputSystem.Touchscreen.current;
var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("PaletteValidationMouse");
var touch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("PaletteValidationTouch");
mouse.MakeCurrent();touch.MakeCurrent();
controller.SetState(Amgen.Deepavali.FlowerPaletteState.Orange);
int stage=0,lastFrame=-1,mouseButtonsPassed=0,samples=0;
bool entered=false,released=false,touchButtonPassed=false,uiBlocked=true,paletteStayed=true;
double stageStart=UnityEngine.Time.realtimeSinceStartupAsDouble;
float selectedMotionPeak=0,otherMotionPeak=0,maxRootDrift=0;
var petalPoint=(UnityEngine.Vector2)UnityEngine.Camera.main.WorldToScreenPoint(motions[0].transform.TransformPoint(new UnityEngine.Vector3(0f,0.02f,-0.24f)));
var errors=new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback logger=(m,t,k)=>{if(k==UnityEngine.LogType.Error||k==UnityEngine.LogType.Exception||k==UnityEngine.LogType.Assert) errors.Add(m);};
UnityEngine.Application.logMessageReceived+=logger;
UnityEditor.SessionState.SetString("FlowerPalette.InputValidation","running");
UnityEditor.EditorApplication.CallbackFunction update=null;
update=()=>
{
    bool finished=false;
    try
    {
        if(!UnityEditor.EditorApplication.isPlaying||controller==null) throw new System.InvalidOperationException("Validation interrupted.");
        if(lastFrame==UnityEngine.Time.frameCount)return;
        lastFrame=UnityEngine.Time.frameCount;samples++;
        double elapsed=UnityEngine.Time.realtimeSinceStartupAsDouble-stageStart;
        if(!entered)
        {
            entered=true;released=false;
            if(stage<7)
            {
                uiBlocked &= !input.TryPulseAtScreenPoint(points[stage]);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{buttons=1,position=points[stage]});
            }
            if(stage==7) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=points[5]});
            if(stage==8) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=2,phase=UnityEngine.InputSystem.TouchPhase.Began,position=petalPoint});
        }
        if(elapsed>0.05&&!released)
        {
            released=true;
            if(stage<7) UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=points[stage]});
            if(stage==7) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=points[5]});
            if(stage==8) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=2,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=petalPoint});
        }
        if(stage==8)
        {
            paletteStayed &= controller.SelectedState==Amgen.Deepavali.FlowerPaletteState.Blue;
            selectedMotionPeak=UnityEngine.Mathf.Max(selectedMotionPeak,motions[0].PulseWeight);
            for(int i=1;i<motions.Length;i++) otherMotionPeak=UnityEngine.Mathf.Max(otherMotionPeak,motions[i].PulseWeight);
        }
        for(int i=0;i<skins.Length;i++) maxRootDrift=UnityEngine.Mathf.Max(maxRootDrift,UnityEngine.Vector3.Distance(rootPositions[i],skins[i].bones[0].position));
        if(elapsed<(stage==8?1.0:0.3)) return;
        if(stage<7&&controller.SelectedState==(Amgen.Deepavali.FlowerPaletteState)stage) mouseButtonsPassed++;
        if(stage==7) touchButtonPassed=controller.SelectedState==Amgen.Deepavali.FlowerPaletteState.Blue;
        if(stage++<8){entered=false;stageStart=UnityEngine.Time.realtimeSinceStartupAsDouble;return;}
        controller.enabled=false;
        bool restored=skins.All(s=>s.sharedMaterial==originalPetal)&&disc.sharedMaterial==originalCenter;
        controller.enabled=true;
        bool retained=controller.SelectedState==Amgen.Deepavali.FlowerPaletteState.Blue&&skins.All(s=>s.sharedMaterial==skins[0].sharedMaterial);
        bool noFlash=UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.FlowerEmissionController>(UnityEngine.FindObjectsSortMode.None).Length==0;
        bool pass=mouseButtonsPassed==7&&touchButtonPassed&&uiBlocked&&paletteStayed&&selectedMotionPeak>0.2f&&otherMotionPeak==0&&maxRootDrift<0.000001f&&restored&&retained&&noFlash&&errors.Count==0;
        string N(float x)=>x.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        string report="{\"passed\":"+pass.ToString().ToLower()+",\"mouseButtonsPassed\":"+mouseButtonsPassed+",\"touchButtonPassed\":"+touchButtonPassed.ToString().ToLower()+
            ",\"uiCannotPulsePetals\":"+uiBlocked.ToString().ToLower()+",\"paletteUnaffectedByPetalTaps\":"+paletteStayed.ToString().ToLower()+",\"selectedMotionPeak\":"+N(selectedMotionPeak)+
            ",\"otherMotionPeak\":"+N(otherMotionPeak)+",\"maxRootDriftMetres\":"+N(maxRootDrift)+",\"materialsRestored\":"+restored.ToString().ToLower()+",\"stateRetainedOnReenable\":"+retained.ToString().ToLower()+
            ",\"noFlashController\":"+noFlash.ToString().ToLower()+",\"runtimeErrors\":"+errors.Count+",\"samples\":"+samples+"}";
        var output=System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-palettes/input-validation.json");
        System.IO.File.WriteAllText(output,report);UnityEditor.SessionState.SetString("FlowerPalette.InputValidation",report);finished=true;
    }
    catch(System.Exception e){UnityEditor.SessionState.SetString("FlowerPalette.InputValidation","FAILED: "+e);finished=true;}
    finally
    {
        if(finished)
        {
            UnityEditor.EditorApplication.update-=update;
            UnityEngine.Application.logMessageReceived-=logger;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
            if(originalMouse!=null&&originalMouse.added)originalMouse.MakeCurrent();if(originalTouch!=null&&originalTouch.added)originalTouch.MakeCurrent();
            controller.SetState(Amgen.Deepavali.FlowerPaletteState.Base);
        }
    }
};
UnityEditor.EditorApplication.update+=update;
return "Testing seven UI buttons, touchscreen selection, stable palettes during petal taps, and material lifecycle.";
