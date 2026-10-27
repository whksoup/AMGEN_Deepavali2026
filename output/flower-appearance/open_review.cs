if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play first.");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Deepavali_PaletteTest.unity");
var controller = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var mesh = controller.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().sharedMesh;
var shader = UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit");
var method = typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
return new { vertices=mesh.vertexCount, uv0=mesh.uv.Length, uv1=mesh.uv2.Length, srpBatcherCode=method.Invoke(null,new object[]{shader,0}), shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray() };
