if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Expected stopped Editor.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var shader=UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit");
var report=new {scene=scene.path,saved=!scene.isDirty,selectedState=c.SelectedState.ToString(),petals=c.PetalCount,symbols=c.SymbolSet.Count,shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray(),runInBackground=UnityEngine.Application.runInBackground,nativeAndroidBuildValidated=false,tabletProfiled=false};
System.IO.File.WriteAllText("output/flower-appearance/final-state.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
