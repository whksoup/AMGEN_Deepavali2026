using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Amgen.Deepavali;

public static class CenterValidation
{
    public static async Task<object> Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var c = UnityEngine.Object.FindFirstObjectByType<FlowerPaletteController>();
        var data = new SerializedObject(c);
        var center = (Renderer)data.FindProperty("centerDisc").objectReferenceValue;
        var originalMaterial = (Material)c.GetType().GetField("originalCenter", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);
        var centerMesh = center.GetComponent<MeshFilter>().sharedMesh;
        var centerMeshId = centerMesh.GetInstanceID();
        var camera = Camera.main; var cameraData = camera.GetUniversalAdditionalCameraData();
        var oldPosition=camera.transform.position; var oldOrtho=camera.orthographic; var oldSize=camera.orthographicSize;
        var oldMask=camera.cullingMask; var oldLayer=center.gameObject.layer; var oldPost=cameraData.renderPostProcessing;
        var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active; var oldPipeline=QualitySettings.renderPipeline;
        var originalState=c.SelectedState;
        var originalPalettes=Enumerable.Range(0,c.PetalCount).Select(i=>c.GetRequestedPetalState(i)).ToArray();
        var originalSymbols=Enumerable.Range(0,c.PetalCount).Select(i=>c.GetSymbol(i)).ToArray();
        var checks=new Dictionary<string,bool>(); var detail=new Dictionary<string,object>(); var errors=new List<string>();
        Application.LogCallback logger=(m,t,k)=>{if(k==LogType.Error||k==LogType.Exception||k==LogType.Assert)errors.Add(m);};
        Application.logMessageReceived+=logger;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var clock=c.GetType().GetField("waveStartedAt",flags);
        var update=(Action)Delegate.CreateDelegate(typeof(Action),c,c.GetType().GetMethod("Update",flags));
        void Step(float p) { clock.SetValue(c,Time.timeAsDouble-c.TransitionDuration*p);update(); }
        var rt=RenderTexture.GetTemporary(400,400,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
        var tex=new Texture2D(400,400,TextureFormat.RGBAFloat,false,true);
        Color[] Capture()
        {
            camera.Render(); RenderTexture.active=rt; tex.ReadPixels(new Rect(0,0,400,400),0,0); tex.Apply(); return tex.GetPixels();
        }
        double Difference(Color a,Color b)=>Math.Max(Math.Abs(a.r-b.r),Math.Max(Math.Abs(a.g-b.g),Math.Abs(a.b-b.b)));
        void SetSetting(string name,float value)
        {
            data.Update(); data.FindProperty(name).floatValue=value; data.ApplyModifiedPropertiesWithoutUndo(); update();
        }
        float ringGlow=data.FindProperty("centerRingGlow").floatValue;
        float iconGlow=data.FindProperty("centerIconGlow").floatValue;
        float dotGlow=data.FindProperty("centerDotGlow").floatValue;
        try
        {
            QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            camera.targetTexture=rt; camera.orthographic=true; camera.orthographicSize=0.145f;
            camera.transform.position=center.bounds.center+Vector3.up; cameraData.renderPostProcessing=false;
            center.gameObject.layer=31; camera.cullingMask=1<<31;
            c.SetState(FlowerPaletteState.Base);
            var mat=center.sharedMaterial; int materialId=mat.GetInstanceID();
            checks["sixPaletteMappingsAndBlankBase"] = c.CenterIconSet.Count==6 && c.GetCenterIcon(FlowerPaletteState.Base)==-1 &&
                Enumerable.Range(1,6).All(i=>c.GetCenterIcon((FlowerPaletteState)i)==i-1) && c.GetCenterIcon((FlowerPaletteState)99)==-1;
            var rects=new HashSet<Vector4>(); bool sourceAndTargetMatch=true, glowUsesFill=true, fillUsesAuthoredColors=true;
            var fillSaturations=new List<float>();
            var imageHashes=new HashSet<string>();
            for(int i=0;i<7;i++)
            {
                var state=(FlowerPaletteState)i;c.SetState(state);c.PaletteSet.TryGet(state,out var palette);
                sourceAndTargetMatch &= mat.GetFloat("_SymbolEnabled")== (i==0?0:1) && mat.GetFloat("_TargetSymbolEnabled")== (i==0?0:1);
                sourceAndTargetMatch &= mat.GetVector("_SymbolRect")==mat.GetVector("_TargetSymbolRect");
                if(i>0)rects.Add(mat.GetVector("_SymbolRect"));
                glowUsesFill &= mat.GetColor("_CenterRingColor")==palette.fill.linear && mat.GetColor("_TargetCenterRingColor")==palette.fill.linear;
                fillUsesAuthoredColors &= mat.GetColor("_BaseColor")==palette.center && mat.GetColor("_TargetBaseColor")==palette.center;
                var pixels=Capture();
                if(i>0)
                {
                    // Empty annulus between artwork and dots: measure actual fill, excluding glow strokes.
                    var sum=Vector3.zero;int count=0;
                    for(int y=0;y<400;y++)for(int x=0;x<400;x++)
                    {
                        float radius=new Vector2(x+0.5f-200,y+0.5f-200).magnitude*(0.29f/400);
                        if(radius<0.067f||radius>0.078f)continue;
                        var pixel=pixels[y*400+x];sum+=new Vector3(pixel.r,pixel.g,pixel.b);count++;
                    }
                    sum/=count;float max=Mathf.Max(sum.x,Mathf.Max(sum.y,sum.z)),min=Mathf.Min(sum.x,Mathf.Min(sum.y,sum.z));
                    fillSaturations.Add((max-min)/Mathf.Max(max,0.00001f));
                }
                imageHashes.Add(Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(tex.GetRawTextureData())));
            }
            checks["sixDistinctAtlasTiles"] = rects.Count==6 && sourceAndTargetMatch;
            checks["sevenDistinctRenderedStates"] = imageHashes.Count==7;
            checks["rimUsesMatchingPaletteFillHue"] = glowUsesFill;
            checks["centerFillConvertsAuthoredColorOnlyOnce"] = fillUsesAuthoredColors;
            checks["allSixRenderedCenterFillsRemainSaturated"] = fillSaturations.All(s=>s>0.7f);
            detail["centerFillSaturation"]=fillSaturations;
            checks["atlasSharedAndMaterialStable"] = mat.GetTexture("_SymbolAtlas")==c.CenterIconSet.Atlas && mat.GetInstanceID()==materialId;
            checks["invalidCenterStatesRejected"] = !c.SetCenterState((FlowerPaletteState)99) && !c.TransitionCenterTo((FlowerPaletteState)(-1));
            c.SetState(FlowerPaletteState.Gold); c.TransitionCenterTo(FlowerPaletteState.Blue); Step(0.08f);
            var activeRect=mat.GetVector("_TargetSymbolRect");
            c.TransitionCenterTo(FlowerPaletteState.Pink); c.TransitionCenterTo(FlowerPaletteState.Orange);
            checks["activeIconNotInterruptedByPendingChoice"] = c.CenterState==FlowerPaletteState.Gold && c.HasPendingTransition && mat.GetVector("_TargetSymbolRect")==activeRect;
            Step(1.01f);
            checks["nextCenterWaveStartsAfterCurrentFinishes"] = c.CenterState==FlowerPaletteState.Blue && c.IsTransitioning && c.TransitionProgress==0;
            Step(1.01f);
            checks["latestCenterChoiceWins"] = c.CenterState==FlowerPaletteState.Orange && !c.IsTransitioning;
            checks["centerOnlyChangePreservesPetalPalettes"] = Enumerable.Range(0,c.PetalCount).All(i=>c.GetPetalState(i)==FlowerPaletteState.Gold);
            c.TransitionSymbol(0,1); c.TransitionTo(FlowerPaletteState.Blue); Step(0.05f);
            c.SetCenterState(FlowerPaletteState.Green);
            checks["immediateCenterChangePreservesPetalWaves"] = c.IsTransitioning && c.IsArtworkTransitioning(0) && c.CenterState==FlowerPaletteState.Green;
            Step(1.01f);
            checks["centerOverrideSurvivesWaveCompletion"] = c.CenterState==FlowerPaletteState.Green;
            c.SetState(FlowerPaletteState.Gold); c.TransitionCenterTo(FlowerPaletteState.Purple);Step(0.07f);
            float progress=c.TransitionProgress;
            SetSetting("centerIconGlow",iconGlow+0.5f);
            checks["brightnessEditPreservesWaveAndDestination"] = c.IsTransitioning && Math.Abs(c.TransitionProgress-progress)<0.0001f && c.SelectedState==FlowerPaletteState.Purple;
            SetSetting("centerIconGlow",iconGlow);

            // Actual disc rendering: target inside the advancing circle, old state outside.
            c.SetState(FlowerPaletteState.Gold); var oldImage=Capture();
            c.SetCenterState(FlowerPaletteState.Blue); var newImage=Capture();
            c.SetCenterState(FlowerPaletteState.Gold); c.TransitionCenterTo(FlowerPaletteState.Blue);
            float soft=mat.GetFloat("_WaveSoftness"), front=0.06f;
            Step((front+soft)/(mat.GetFloat("_WaveRadius")+2*soft)); var midImage=Capture();
            int inner=0,outer=0,mismatch=0;
            for(int y=0;y<400;y++)for(int x=0;x<400;x++)
            {
                float radius=new Vector2(x+0.5f-200,y+0.5f-200).magnitude*(0.29f/400);
                int n=y*400+x;
                if(radius<front-soft-0.008f) {inner++; if(Difference(midImage[n],newImage[n])>0.002)mismatch++;}
                if(radius>front+soft+0.008f && radius<0.116f) {outer++;if(Difference(midImage[n],oldImage[n])>0.002)mismatch++;}
            }
            checks["gpuCenterArtworkAndColorRevealOutward"] = inner>100 && outer>100 && mismatch==0;
            detail["wavePixels"]=new {inner,outer,mismatch};
            Step(1.01f);

            // Zero the rim glow only: changed pixels must lie at its circular edge.
            c.SetState(FlowerPaletteState.Gold);
            SetSetting("centerRingGlow",0);var noRingGlow=Capture();
            SetSetting("centerRingGlow",ringGlow);var withRingGlow=Capture();
            double ringDifference=0,interiorDifference=0;int ringSamples=0,interiorSamples=0;
            for(int y=0;y<400;y++)for(int x=0;x<400;x++)
            {
                float radius=new Vector2(x+0.5f-200,y+0.5f-200).magnitude*(0.29f/400);
                double difference=Difference(noRingGlow[y*400+x],withRingGlow[y*400+x]);
                if(radius>0.106f&&radius<0.116f){ringDifference+=difference;ringSamples++;}
                if(radius<0.08f){interiorDifference+=difference;interiorSamples++;}
            }
            ringDifference/=ringSamples;interiorDifference/=interiorSamples;
            checks["emissionRingIsNearRadialEdgeOnly"] = ringDifference>0.02 && interiorDifference<0.00001;
            detail["ringEmissionDifference"]=ringDifference;detail["interiorDifferenceWhenRingGlowChanges"]=interiorDifference;
            SetSetting("centerIconGlow",0);var noIconGlow=Capture();
            SetSetting("centerIconGlow",iconGlow);var withIconGlow=Capture();
            double iconDifference=noIconGlow.Select((p,i)=>Difference(p,withIconGlow[i])).Average();
            checks["iconEmissionIndependentlyAdjustable"] = iconDifference>0.001;
            detail["iconEmissionDifference"]=iconDifference;
            cameraData.renderPostProcessing=true;var post=Capture();cameraData.renderPostProcessing=false;
            checks["postProcessingAffectsCenterGlow"] = post.Select((p,i)=>Difference(p,withIconGlow[i])).Average()>0.001;

            c.TransitionTo(FlowerPaletteState.Blue);for(int i=0;i<c.PetalCount;i++)c.TransitionSymbol(i,2);
            for(int i=0;i<16;i++)update();long allocation=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<256;i++)update();allocation=GC.GetAllocatedBytesForCurrentThread()-allocation;
            checks["combinedUpdatesAllocateZeroManagedBytes"] = allocation==0;detail["allocatedBytesFor256Updates"]=allocation;
            var shader=mat.shader;
            checks["shaderCompilesWithoutErrors"] = ShaderUtil.GetShaderMessages(shader).Length==0;
            var batch=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            checks["srpBatcherCompatible"]=(int)batch.Invoke(null,new object[]{shader,0})==0;
            checks["centerGeometryUnchanged"] = center.GetComponent<MeshFilter>().sharedMesh.GetInstanceID()==centerMeshId;
            c.TransitionCenterTo(FlowerPaletteState.Orange); c.enabled=false;
            checks["originalCenterMaterialRestored"] = center.sharedMaterial==originalMaterial;
            await Task.Delay(50); c.enabled=true; await Task.Delay(50);
            checks["latestCenterIconRetainedAfterReenable"] = c.CenterState==FlowerPaletteState.Orange && center.sharedMaterial.GetFloat("_TargetSymbolEnabled")==1;
            int owned=Resources.FindObjectsOfTypeAll<Material>().Count(m=>m.name.StartsWith("Flower Petal ")||m.name=="Flower Center (Runtime)");
            checks["noNewMaterialLeaks"] = owned==c.PetalCount+1;detail["ownedMaterials"]=owned;
            var atlas=c.CenterIconSet.Atlas;
            checks["atlasUsesCompressedDesktopStorage"] = atlas.format==TextureFormat.BC4;
            detail["atlas"]=new{atlas.width,atlas.height,format=atlas.format.ToString(),atlas.mipmapCount};
        }
        catch(Exception e){checks["completed"]=false;errors.Add(e.ToString());}
        finally
        {
            if(!c.enabled)c.enabled=true;
            SetSetting("centerRingGlow",ringGlow);SetSetting("centerIconGlow",iconGlow);SetSetting("centerDotGlow",dotGlow);
            c.SetState(originalState);for(int i=0;i<c.PetalCount;i++){c.SetPetalState(i,originalPalettes[i]);c.SetSymbol(i,originalSymbols[i]);}
            center.gameObject.layer=oldLayer;camera.cullingMask=oldMask;camera.transform.position=oldPosition;
            camera.orthographic=oldOrtho;camera.orthographicSize=oldSize;cameraData.renderPostProcessing=oldPost;
            camera.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);
            QualitySettings.renderPipeline=oldPipeline;Application.logMessageReceived-=logger;
        }
        checks["noRuntimeErrors"]=errors.Count==0;
        var report=new{passed=checks.Values.All(x=>x),checks,detail,errors};
        File.WriteAllText("output/flower-center/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
