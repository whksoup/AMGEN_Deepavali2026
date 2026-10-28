// Run through Unity MCP eval_file in Edit Mode. Rebuilds from the unchanged FBX.
// Only the shared inner-petal prefab is changed; nested flower prefabs inherit it.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
const string root = "Assets/HK_Signature/Deepavali/PetalPrototype/";
const string meshPath = root + "Models/Inner Petal Shingled.asset";
const string prefabPath = root + "Prefabs/Deepavali Inner Petal.prefab";
var source = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(root + "Models/Deepavali_SinglePetal.fbx")
    .OfType<UnityEngine.Mesh>().Single(m => m.name == "InnerPetal_01");
var mesh = UnityEngine.Object.Instantiate(source);
mesh.name = "InnerPetal_01_Shingled";
var vertices = mesh.vertices;
// Shear the surface in height only: preserve its top-down outline, UVs and skinning.
float slope = UnityEngine.Mathf.Tan(6f * UnityEngine.Mathf.Deg2Rad);
for (int i = 0; i < vertices.Length; i++) vertices[i].z += vertices[i].x * slope;
mesh.vertices = vertices;
mesh.RecalculateNormals();
mesh.RecalculateTangents();
mesh.RecalculateBounds();
var savedMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(meshPath);
if (savedMesh == null) { UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath); savedMesh = mesh; }
else { UnityEditor.EditorUtility.CopySerialized(mesh, savedMesh); UnityEngine.Object.DestroyImmediate(mesh); }
var backup = System.IO.Path.GetFullPath("output/flower-top-shingle/before-inner-petal.prefab");
if (!System.IO.File.Exists(backup)) System.IO.File.Copy(prefabPath, backup);
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var skin = prefab.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
    skin.sharedMesh = savedMesh;
    var motion = prefab.GetComponent<Amgen.Deepavali.PetalMotionController>();
    var serialized = new UnityEditor.SerializedObject(motion);
    serialized.FindProperty("liftBias").vector3Value = new UnityEngine.Vector3(0.3f, 1f, 2f);
    serialized.FindProperty("idleLift").vector3Value = new UnityEngine.Vector3(0.15f, 0.6f, 1.8f);
    serialized.FindProperty("idleSway").vector3Value = new UnityEngine.Vector3(0.05f, 0.3f, 1.5f);
    serialized.FindProperty("pulseLift").vector3Value = new UnityEngine.Vector3(0.25f, 1.5f, 10f);
    serialized.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
UnityEditor.EditorUtility.SetDirty(savedMesh);
UnityEditor.AssetDatabase.SaveAssets();
return new { meshPath, prefabPath, vertices = savedMesh.vertexCount, shingleDegrees = 6 };
