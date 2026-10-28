if (Application.isPlaying) throw new System.InvalidOperationException("Import in Edit Mode after the other chat releases Unity.");
const string folder="Assets/HK_Signature/Deepavali/PetalPrototype/Symbols/Rootward Inner";
var staged=System.IO.Directory.GetFiles("output/inner-petal-pattern-guides/staged/Decorated Masks","*.png");
if(staged.Length!=8)throw new System.Exception("Expected all eight staged masks.");
foreach(var path in staged)
    System.IO.File.Copy(path,folder+"/Decorated Masks/"+System.IO.Path.GetFileName(path),true);
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var source=AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>(folder+"/Rootward Inner Source.asset");
if(source==null)throw new System.Exception("The existing startup atlas source is missing.");
var set=Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.Build(source);
return new { masks=staged.Length,atlas=AssetDatabase.GetAssetPath(set.Atlas),tiles=set.Count,size=set.Atlas.width+"x"+set.Atlas.height,sceneSaved=false };
