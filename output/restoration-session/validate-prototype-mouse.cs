// Drive the real Input System Update path with button-free mouse events.
var r=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.ScreenRestorationController>();
if(!Application.isPlaying || r==null)throw new System.Exception("Restoration preview must be playing.");
var preview=r.GetComponent<Amgen.Deepavali.RestorationPreview>();
var savedInput=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
var savedBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
var oldMouse=UnityEngine.InputSystem.Mouse.current;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("RestorationTestMouse");mouse.MakeCurrent();
r.ResetRestoration();r.useMouseInput=true;preview.showControls=true;
var checks=new System.Collections.Generic.Dictionary<string,bool>();
var errors=new System.Collections.Generic.List<string>();
void Check(string name,bool pass){checks[name]=pass;if(!pass)throw new System.Exception(name);}
void Move(Vector2 point)=>UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=point});
Vector2 ScreenPoint(float x,float y){var rect=r.interactionCamera.pixelRect;return new Vector2(rect.x+x*rect.width,rect.y+y*rect.height);}
Move(ScreenPoint(.5f,.94f));
int stage=0,frame=Time.frameCount;double deadline=Time.realtimeSinceStartupAsDouble+.6;
float coverage=0;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool done=false;
    try
    {
        if(!Application.isPlaying)throw new System.Exception("Play Mode ended.");
        if(Time.frameCount==frame || Time.realtimeSinceStartupAsDouble<deadline)return;frame=Time.frameCount;
        switch(stage)
        {
            case 0:Check("Preview controls do not paint",r.Coverage==0);Move(ScreenPoint(.25f,.5f));break;
            case 1:
                Check("Mouse hover restores without a click",r.SampleRestoration(new Vector2(.25f,.5f))>.95f && !mouse.leftButton.isPressed && r.HasPointer);
                Move(new Vector2(-50,-50));break;
            case 2:Check("Leaving viewport ends presence",!r.HasPointer);coverage=r.Coverage;break;
            case 3:Check("Outside mouse stops painting",r.Coverage==coverage);Move(ScreenPoint(.75f,.5f));break;
            case 4:
                Check("Returning mouse paints at new position",r.SampleRestoration(new Vector2(.75f,.5f))>.95f);
                Check("Earlier hover remains restored",r.SampleRestoration(new Vector2(.25f,.5f))>.95f);
                Check("Re-entry does not connect through the scene",r.SampleRestoration(new Vector2(.5f,.5f))==0);
                done=true;break;
        }
        stage++;deadline=Time.realtimeSinceStartupAsDouble+.65;
    }
    catch(System.Exception e){errors.Add(e.ToString());done=true;}
    finally
    {
        if(done)
        {
            UnityEditor.EditorApplication.update-=tick;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);if(oldMouse!=null)oldMouse.MakeCurrent();
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=savedInput;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=savedBackground;
            r.useMouseInput=false;r.ResetRestoration();preview.showControls=false;
            var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0&&checks.Values.All(v=>v),checks,errors},Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("output/restoration-session/prototype-regression/mouse-validation.json",json);
            UnityEditor.SessionState.SetString("Restoration.MouseValidation",json);
        }
    }
};
UnityEditor.SessionState.SetString("Restoration.MouseValidation","running");UnityEditor.EditorApplication.update+=tick;
return "Started button-free mouse input validation";

