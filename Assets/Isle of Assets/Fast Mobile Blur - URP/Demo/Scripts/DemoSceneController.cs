using UnityEngine;
using UnityEngine.UI;

namespace FastMobileBlurURP2023.Demo
{
    [HelpURL("https://assetstore.unity.com/packages/slug/242848")]
    public class DemoSceneController : MonoBehaviour
    {
        [SerializeField]
        private Toggle isActive = null, increaseKernelSize = null;

        [SerializeField]
        private Slider passes = null, intensity = null;

        /// <summary>
        /// Setting a parameter based on data from the UI
        /// </summary>
        public void SetIsActiveState()
        {
            BlurURP.Settings.IsActive = isActive.isOn;
        }

        /// <summary>
        /// Setting a parameter based on data from the UI
        /// </summary>
        public void SetIncreaseKernelSizeState()
        {
            BlurURP.Settings.IncreaseKernelSize = increaseKernelSize.isOn;
        }

        /// <summary>
        /// Setting a parameter based on data from the UI
        /// </summary>
        public void SetPassesValue()
        {
            BlurURP.Settings.Passes = (int)passes.value;
        }

        /// <summary>
        /// Setting a parameter based on data from the UI
        /// </summary>
        public void SetIntensityValue()
        {
            BlurURP.Settings.Intensity = intensity.value;
        }

        /// <summary>
        /// Setting parameters based on data from the UI
        /// </summary>
        private void Start()
        {
            isActive.isOn = BlurURP.Settings.IsActive;
            increaseKernelSize.isOn = BlurURP.Settings.IncreaseKernelSize;
            passes.value = BlurURP.Settings.Passes;
            intensity.value = BlurURP.Settings.Intensity;
        }
    }
}