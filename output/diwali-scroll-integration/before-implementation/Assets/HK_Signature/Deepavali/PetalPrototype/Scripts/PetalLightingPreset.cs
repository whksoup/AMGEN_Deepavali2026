using UnityEngine;

namespace Amgen.Deepavali
{
    [CreateAssetMenu(menuName = "Amgen/Deepavali/Petal Lighting Prototype")]
    public sealed class PetalLightingPreset : ScriptableObject
    {
        public Texture2D innerMap, outerMap;
        public PetalLightingSettings inner = new PetalLightingSettings();
        public PetalLightingSettings outer = new PetalLightingSettings { seed = 17 };
    }
}
