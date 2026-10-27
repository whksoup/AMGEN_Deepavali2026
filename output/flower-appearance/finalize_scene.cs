if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop play before saving.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/Deepavali_PaletteTest.unity")throw new System.InvalidOperationException("Wrong scene.");
UnityEngine.Camera.main.transform.position=new UnityEngine.Vector3(0,1.65f,0.11f);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
return new {scene=scene.path,saved=!scene.isDirty};
