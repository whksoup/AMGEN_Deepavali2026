// Run via Unity MCP eval_file in Edit Mode. All generated meshes/materials are saved assets.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Edit Mode required.");
const string root = "Assets/HK_Signature/Deepavali/Background";
System.IO.Directory.CreateDirectory(root + "/Materials");
System.IO.Directory.CreateDirectory(root + "/Models");
System.IO.Directory.CreateDirectory(root + "/Prefabs");
UnityEditor.AssetDatabase.Refresh();

UnityEngine.Color Hex(string value) { UnityEngine.ColorUtility.TryParseHtmlString(value, out var c); return c; }
UnityEngine.Material Material(string name, string shader, UnityEngine.Color color, float metal = 0, float smooth = 0.2f)
{
    string path = root + "/Materials/" + name + ".mat";
    var m = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if (m == null) { m = new UnityEngine.Material(UnityEngine.Shader.Find(shader)); UnityEditor.AssetDatabase.CreateAsset(m,path); }
    m.shader = UnityEngine.Shader.Find(shader);
    m.SetColor("_BaseColor", color); m.SetFloat("_Metallic",metal); m.SetFloat("_Smoothness",smooth);
    m.enableInstancing = true; UnityEditor.EditorUtility.SetDirty(m); return m;
}
UnityEngine.Mesh SaveMesh(UnityEngine.Mesh mesh, string name)
{
    mesh.name = name; mesh.RecalculateNormals(); mesh.RecalculateBounds();
    string path = root + "/Models/" + name + ".asset";
    var saved = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
    if (saved == null) { UnityEditor.AssetDatabase.CreateAsset(mesh,path); return mesh; }
    // Assign mesh buffers explicitly; CopySerialized can leave a stale GPU buffer after repair.
    saved.Clear(); saved.vertices=mesh.vertices; saved.normals=mesh.normals; saved.uv=mesh.uv;
    saved.subMeshCount=mesh.subMeshCount;
    for(int i=0;i<mesh.subMeshCount;i++) saved.SetTriangles(mesh.GetTriangles(i),i);
    saved.RecalculateBounds(); saved.UploadMeshData(false);
    UnityEditor.EditorUtility.SetDirty(saved); UnityEngine.Object.DestroyImmediate(mesh); return saved;
}
var soilMat = Material("Dark Earthen Soil", "Amgen/Deepavali/Festival Soil", Hex("#766958"));
soilMat.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/YughuesFreeGroundMaterials/Textures/T_YFGM_SoilGravel04_d.tga"));
soilMat.SetTexture("_BumpMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/YughuesFreeGroundMaterials/Textures/T_YFGM_SoilGravel04_n.tga"));
soilMat.SetFloat("_Tiling",3.2f); soilMat.SetFloat("_BumpScale",0.65f); soilMat.SetFloat("_PoolStrength",0.07f);
var clay = Material("Diya Burnished Terracotta","Universal Render Pipeline/Lit",Hex("#8B3617"),0.12f,0.28f);
var brass = Material("Diya Antique Brass Rim","Universal Render Pipeline/Lit",Hex("#96612A"),0.4f,0.3f);
var wick = Material("Diya Cotton Wick","Universal Render Pipeline/Lit",Hex("#B58A4A"),0,0.1f);
var oil = Material("Diya Amber Oil","Universal Render Pipeline/Lit",Hex("#391904"),0.05f,0.65f);
oil.SetFloat("_SpecularHighlights",0); oil.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
oil.EnableKeyword("_EMISSION"); oil.SetColor("_EmissionColor",new UnityEngine.Color(0.06f,0.012f,0.001f));
var ember = Material("Diya Wick Ember","Universal Render Pipeline/Lit",Hex("#FFB02E"));
ember.EnableKeyword("_EMISSION"); ember.SetColor("_EmissionColor",new UnityEngine.Color(4,1.2f,0.04f));
var flameMat=Material("Diya Living Flame","Amgen/Deepavali/Diya Flame",UnityEngine.Color.white);
flameMat.SetColor("_Tint",new UnityEngine.Color(5,1.8f,0.2f,1));
var orange = Material("Marigold Saffron","Universal Render Pipeline/Lit",Hex("#C44D05"),0,0.16f);
var orangeLight = Material("Marigold Gold Tips","Universal Render Pipeline/Lit",Hex("#ED7C0A"),0,0.14f);
var orangeDark = Material("Marigold Burnt Heart","Universal Render Pipeline/Lit",Hex("#7B2508"),0,0.12f);
var red = Material("Burgundy Petals","Universal Render Pipeline/Lit",Hex("#690C28"),0,0.18f);
var redLight = Material("Rose Petal Tips","Universal Render Pipeline/Lit",Hex("#A62048"),0,0.16f);
var redDark = Material("Burgundy Flower Heart","Universal Render Pipeline/Lit",Hex("#380D15"),0,0.12f);
foreach(var m in new[]{orange,orangeLight,orangeDark,red,redLight,redDark}) m.SetFloat("_Cull",0);
var rice = Material("Ivory Rangoli Grains","Universal Render Pipeline/Lit",Hex("#BD9B6B"),0,0.05f);

// Cupped, folded petal surfaces, shared by every flower instance.
UnityEngine.Mesh FlowerMesh(bool loose)
{
    var vertices=new System.Collections.Generic.List<UnityEngine.Vector3>();
    var uv=new System.Collections.Generic.List<UnityEngine.Vector2>();
    var triangles=new[]{new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>()};
    int rings=loose?1:4;
    for(int ring=0;ring<rings;ring++)
    {
        int count=loose?1:14-ring*2;
        float length=loose?0.5f:0.32f-ring*0.055f;
        float inner=loose?0:0.16f-ring*0.05f;
        for(int p=0;p<count;p++)
        {
            float angle=(p/(float)count + ring*0.037f)*UnityEngine.Mathf.PI*2;
            var radial=new UnityEngine.Vector3(UnityEngine.Mathf.Cos(angle),0,UnityEngine.Mathf.Sin(angle));
            var across=new UnityEngine.Vector3(-radial.z,0,radial.x);
            int start=vertices.Count;
            for(int u=0;u<=7;u++) for(int v=0;v<=6;v++)
            {
                float t=u/7f, s=v/3f-1;
                float width=UnityEngine.Mathf.Pow(UnityEngine.Mathf.Max(0,UnityEngine.Mathf.Sin(t*UnityEngine.Mathf.PI)),0.6f)*(loose?0.16f:0.105f-ring*0.017f);
                float height=ring*0.045f + UnityEngine.Mathf.Sin(t*UnityEngine.Mathf.PI)*0.065f + t*t*0.025f
                    + s*s*0.04f + 0.01f*UnityEngine.Mathf.Cos(s*UnityEngine.Mathf.PI*3)*UnityEngine.Mathf.Sin(t*UnityEngine.Mathf.PI);
                vertices.Add(radial*(inner+t*length)+across*(s*width)+UnityEngine.Vector3.up*height);
                uv.Add(new UnityEngine.Vector2(t,(s+1)*0.5f));
            }
            var tris=triangles[loose?0:ring==3?2:(p+ring)%2];
            for(int u=0;u<7;u++) for(int v=0;v<6;v++)
            {
                int a=start+u*7+v,b=a+7;
                tris.Add(a);tris.Add(a+1);tris.Add(b);tris.Add(a+1);tris.Add(b+1);tris.Add(b);
            }
        }
    }
    var mesh=new UnityEngine.Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=loose?1:3;
    for(int i=0;i<mesh.subMeshCount;i++)mesh.SetTriangles(triangles[i],i);
    return SaveMesh(mesh,loose?"Loose Curled Petal":"Layered Marigold");
}
var flowerMesh=FlowerMesh(false); var looseMesh=FlowerMesh(true);
var old=UnityEngine.GameObject.Find("Deepavali Earthen Background");
if(old!=null) UnityEngine.Object.DestroyImmediate(old);
var background=new UnityEngine.GameObject("Deepavali Earthen Background");
UnityEditor.Undo.RegisterCreatedObjectUndo(background,"Set Deepavali background");
var layout=background.AddComponent<Amgen.Deepavali.DeepavaliBackground>();
var oldGround=UnityEngine.GameObject.Find("Ground");
if(oldGround!=null){UnityEditor.Undo.RecordObject(oldGround,"Replace plain ground");oldGround.SetActive(false);}
var floor=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);floor.name="Earth Floor - Yughues Soil Gravel 04";
floor.transform.SetParent(background.transform,false);floor.transform.localPosition=new UnityEngine.Vector3(0,-0.08f,0);floor.transform.localScale=UnityEngine.Vector3.one*0.6f;
UnityEngine.Object.DestroyImmediate(floor.GetComponent<UnityEngine.Collider>());layout.soil=floor.GetComponent<UnityEngine.Renderer>();layout.soil.sharedMaterial=soilMat;

var lampSource=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/DiwaliAssets/cgtrader_optimized_Oil+Lamp.fbx");
var lampNames=new[]{"Upper Left Diya","Upper Right Diya","Lower Left Diya","Lower Right Diya"};
layout.lamps=new UnityEngine.Transform[4];layout.flames=new UnityEngine.Transform[4];layout.lampLights=new UnityEngine.Light[4];
for(int i=0;i<4;i++)
{
    var holder=new UnityEngine.GameObject(lampNames[i]);holder.transform.SetParent(background.transform,false);layout.lamps[i]=holder.transform;
    var lamp=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(lampSource);lamp.transform.SetParent(holder.transform,false);
    float scale=0.14f/5.08f;
    lamp.transform.localPosition=lampSource.transform.localPosition*scale;
    lamp.transform.localScale=lampSource.transform.localScale*scale;
    lamp.transform.localRotation=lampSource.transform.localRotation;
    lamp.name="Oil Lamp - supplied FBX";
    lamp.GetComponent<UnityEngine.Renderer>().sharedMaterials=new[]{clay,brass,wick,ember,oil};
    // A camera-facing flame makes the fire legible from this overhead camera.
    var flame=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Quad);flame.name="Warm Flickering Flame";
    UnityEngine.Object.DestroyImmediate(flame.GetComponent<UnityEngine.Collider>());
    flame.transform.SetParent(holder.transform,false);flame.transform.localPosition=new UnityEngine.Vector3(0,0.041f,-0.010f);
    flame.transform.localScale=new UnityEngine.Vector3(0.019f,0.037f,1);
    var fr=flame.GetComponent<UnityEngine.Renderer>();fr.sharedMaterial=flameMat;fr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;fr.receiveShadows=false;
    layout.flames[i]=flame.transform;
    var lightObject=new UnityEngine.GameObject("Amber Candle Light");lightObject.transform.SetParent(holder.transform,false);lightObject.transform.localPosition=new UnityEngine.Vector3(0,0.065f,0);
    var light=lightObject.AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Point;light.color=Hex("#FFAA42");light.range=0.32f;light.intensity=0.005f;light.shadows=UnityEngine.LightShadows.None;
    lightObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
    layout.lampLights[i]=light;
}

var dressing=new UnityEngine.GameObject("Scattered Marigolds and Burgundy Petals");dressing.transform.SetParent(background.transform,false);
var pieces=new System.Collections.Generic.List<Amgen.Deepavali.DeepavaliBackground.BorderPiece>();
var random=new System.Random(20261028);
float Next(float min,float max) {return min+(float)random.NextDouble()*(max-min);}
for(int i=0;i<210;i++)
{
    bool whole=i<58, grain=i>=170;
    float x=Next(0.025f,0.975f),y=Next(0.025f,0.975f);
    // Most detail sits along the sides; a few pieces finish the top/bottom margins.
    if (i%5!=0) x=i%2==0?Next(0.035f,0.26f):Next(0.74f,0.965f);
    var piece=new UnityEngine.GameObject((whole?"Marigold ":grain?"Rangoli Grain ":"Loose Petal ")+i.ToString("000"));piece.transform.SetParent(dressing.transform,false);
    var filter=piece.AddComponent<UnityEngine.MeshFilter>();filter.sharedMesh=whole?flowerMesh:looseMesh;
    var renderer=piece.AddComponent<UnityEngine.MeshRenderer>();
    bool burgundy=i%3==0;
    renderer.sharedMaterials=whole?(burgundy?new[]{red,redLight,redDark}:new[]{orange,orangeLight,orangeDark}):new[]{grain?rice:burgundy?redLight:orangeLight};
    float size=whole?Next(0.024f,0.067f):grain?Next(0.002f,0.005f):Next(0.007f,0.018f);
    piece.transform.localScale=UnityEngine.Vector3.one*size;piece.transform.localRotation=UnityEngine.Quaternion.Euler(0,Next(0,360),0);
    renderer.shadowCastingMode=whole?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;
    pieces.Add(new Amgen.Deepavali.DeepavaliBackground.BorderPiece{transform=piece.transform,viewport=new UnityEngine.Vector2(x,y),clearance=size*0.5f});
}
layout.border=pieces.ToArray();layout.RefreshLayout(UnityEngine.Camera.main);
UnityEditor.EditorUtility.SetDirty(layout);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(background,root+"/Prefabs/Deepavali Earthen Background.prefab",UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return new {lamps=layout.lamps.Length,borderPieces=layout.border.Length,visiblePieces=layout.border.Count(p=>p.transform.gameObject.activeSelf),floor=UnityEditor.AssetDatabase.GetAssetPath(soilMat)};
