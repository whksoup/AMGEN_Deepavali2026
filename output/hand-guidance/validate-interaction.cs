var h = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.HandHereGraphic>();
var interaction = h.GetComponent<Amgen.Deepavali.HandHereInteraction>();
var camera = h.canvas.worldCamera;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var clock = typeof(Amgen.Deepavali.HandHereInteraction).GetField("startedAt",flags);
var update = typeof(Amgen.Deepavali.HandHereInteraction).GetMethod("Update",flags);
var eventSystem = UnityEngine.EventSystems.EventSystem.current;
var evidence = new System.Collections.Generic.List<string>();
System.Action<bool,string> check = (ok,message) => { if(!ok) throw new System.Exception(message); evidence.Add(message); };
UnityEngine.Canvas.ForceUpdateCanvases();
var point = UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,h.rectTransform.position);
var pointer = new UnityEngine.EventSystems.PointerEventData(eventSystem) {position=point, button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
eventSystem.RaycastAll(pointer,hits);
check(hits.Count>0 && hits[0].gameObject==h.gameObject,"UI raycast selects hand badge");
check(Amgen.Deepavali.HandHereInteraction.BlocksScreenPoint(point),"Active badge blocks petal input");
var corner = h.rectTransform.TransformPoint(h.rectTransform.rect.max);
check(!h.Raycast(UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,corner),camera),"Transparent corners do not intercept clicks");
var clone = UnityEngine.Object.Instantiate(h.gameObject,h.transform.parent);
try
{
    var other = clone.GetComponent<Amgen.Deepavali.HandHereGraphic>();
    UnityEngine.EventSystems.ExecuteEvents.Execute(h.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
    check(interaction.IsDismissing,"Pointer click starts loading");
    var started=(double)clock.GetValue(interaction);
    interaction.Dismiss();
    check((double)clock.GetValue(interaction)==started,"Repeated click does not restart timer");
    check(h.material!=other.material && other.material.GetFloat("_Loading")==0,"Duplicate retains independent idle material");
}
finally { UnityEngine.Object.DestroyImmediate(clone); }

var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;var oldAspect=camera.aspect; var width=camera.pixelWidth;var height=camera.pixelHeight;
try
{
    foreach(var stage in new[]{new UnityEngine.Vector2(0,0),new UnityEngine.Vector2(1,0.32f),new UnityEngine.Vector2(2,0.95f),new UnityEngine.Vector2(3,1.25f)})
    {
        clock.SetValue(interaction,UnityEngine.Time.unscaledTimeAsDouble-stage.y);update.Invoke(interaction,null);
        var rt=UnityEngine.RenderTexture.GetTemporary(width,height,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
        var readback=new UnityEngine.Texture2D(width,height,UnityEngine.TextureFormat.RGBAFloat,false,true);
        var png=new UnityEngine.Texture2D(width,height,UnityEngine.TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.aspect=oldAspect;UnityEngine.Canvas.ForceUpdateCanvases();camera.Render();UnityEngine.RenderTexture.active=rt;
            readback.ReadPixels(new UnityEngine.Rect(0,0,width,height),0,0);readback.Apply();var pixels=readback.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
            png.SetPixels(pixels);png.Apply();
            var p=camera.WorldToScreenPoint(h.rectTransform.position);
            var detail=new UnityEngine.Texture2D(300,300,UnityEngine.TextureFormat.RGB24,false);
            try{detail.SetPixels(png.GetPixels((int)p.x-150,(int)p.y-150,300,300));detail.Apply();System.IO.File.WriteAllBytes("output/hand-guidance/despawn-stage-"+(int)stage.x+".png",detail.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(detail);}
        }
        finally{camera.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(readback);UnityEngine.Object.DestroyImmediate(png);}
    }
}
finally{camera.aspect=oldAspect;UnityEngine.Canvas.ForceUpdateCanvases();}
clock.SetValue(interaction,UnityEngine.Time.unscaledTimeAsDouble-2);update.Invoke(interaction,null);
check(!h.gameObject.activeSelf,"Completion deactivates prompt");
check(!Amgen.Deepavali.HandHereInteraction.BlocksScreenPoint(point),"Hidden prompt releases input");
h.gameObject.SetActive(true);
check(!interaction.IsDismissing && h.material.GetFloat("_Dissolve")==0 && h.material.GetFloat("_Loading")==0,"Re-enable resets the prompt");
check(UnityEditor.ShaderUtil.GetShaderMessages(h.material.shader).Length==0,"No shader compiler messages");
System.IO.File.WriteAllLines("output/hand-guidance/interaction-validation.txt",evidence);
return evidence;

