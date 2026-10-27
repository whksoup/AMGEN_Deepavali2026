using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static FastMobileBlurURP2023.BlurURP;

namespace FastMobileBlurURP2023
{
    /// <summary>
    /// Blur settings for UniversalRenderPipelineAsset_Renderer
    /// </summary>
    [HelpURL("https://assetstore.unity.com/packages/slug/242848")]
    public class BlurURP : ScriptableRendererFeature
    {
        private class BlurPass : ScriptableRenderPass
        {
            private static RenderTargetIdentifier[] temps = new RenderTargetIdentifier[2];
            private static RenderTargetIdentifier blurTexture;

            private static readonly string keyword = "KERNELSIZE";
            private static readonly int[,] data = new int[5, 3] { { 2, 0, 0 }, { 2, 4, 0 }, { 4, 8, 0 }, { 4, 8, 16 }, { 8, 16, 32 } };
            private static readonly int[] tempID = { Shader.PropertyToID("_BlurTemp"), Shader.PropertyToID("_BlurTemp1") };
            private static int intensityID = Shader.PropertyToID("_Intensity"), maskTextureID = Shader.PropertyToID("_MaskTex"), blurTextureID = Shader.PropertyToID("_BlurTex"), tempCopyID = Shader.PropertyToID("_TempCopy");

            private RenderTargetIdentifier source;
            private string profilerTag;
            private bool isActive, increaseKernelSize;
            private int passes;
            private float intensity;
            private Texture2D maskTexture, prevMaskTexture;
            private Material material;

            /// <summary>
            /// Initializing this instance
            /// </summary>
            /// <param name="renderPassEvent"></param>
            /// <param name="profilerTag"></param>
            /// <param name="intensity"></param>
            /// <param name="increaseKernelSize"></param>
            /// <param name="maskTexture"></param>
            /// <param name="passes"></param>
            /// <param name="isActive"></param>
            /// <param name="material"></param>
            public BlurPass(RenderPassEvent renderPassEvent, string profilerTag, float intensity, bool increaseKernelSize, Texture2D maskTexture, int passes, bool isActive, Material material)
            {
                for (int i = 0; i < temps.Length; i++)
                    temps[i] = new RenderTargetIdentifier(tempID[i]);
                blurTexture = new RenderTargetIdentifier(blurTextureID);
                this.renderPassEvent = renderPassEvent;
                this.profilerTag = profilerTag;
                this.intensity = intensity;
                this.increaseKernelSize = increaseKernelSize;
                this.maskTexture = maskTexture == null ? Texture2D.whiteTexture : maskTexture;
                this.passes = passes;
                this.isActive = isActive;
                this.material = material;
            }

            /// <summary>
            /// Setting RenderTargetIdentifier source
            /// </summary>
            /// <param name="source"></param>
            public void Setup(RenderTargetIdentifier source)
            {
                this.source = source;
            }

            /// <summary>
            /// Execute the pass. This is where custom rendering occurs
            /// </summary>
            /// <param name="context"></param>
            /// <param name="renderingData"></param>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!isActive || intensity == 0f || material == null)
                {
#if UNITY_EDITOR
                    if (material == null)
                        Debug.LogWarning("Please specify the blur material");
#endif
                    return;
                }
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;
                cmd.GetTemporaryRT(tempCopyID, opaqueDesc, FilterMode.Bilinear);
                cmd.Blit(BuiltinRenderTextureType.CameraTarget, tempCopyID);
                if (increaseKernelSize)
                    material.EnableKeyword(keyword);
                else
                    material.DisableKeyword(keyword);
                material.SetFloat(intensityID, intensity);
                if (maskTexture != null || prevMaskTexture != maskTexture)
                {
                    prevMaskTexture = maskTexture;
                    material.SetTexture(maskTextureID, maskTexture);
                }
                cmd.GetTemporaryRT(blurTextureID, Screen.width / data[passes - 1, 0], Screen.height / data[passes - 1, 0], 0, FilterMode.Bilinear);
                if (passes >= 2)
                    cmd.GetTemporaryRT(tempID[0], Screen.width / data[passes - 1, 1], Screen.height / data[passes - 1, 1], 0, FilterMode.Bilinear);
                if (passes >= 4)
                    cmd.GetTemporaryRT(tempID[1], Screen.width / data[passes - 1, 2], Screen.height / data[passes - 1, 2], 0, FilterMode.Bilinear);
                if (passes == 1 || passes == 3 || passes == 5)
                {
                    cmd.Blit(source, blurTexture, material, 0);
                    if (passes == 3 || passes == 5)
                        cmd.Blit(blurTexture, temps[0], material, 0);
                }
                else
                    cmd.Blit(source, temps[0], material, 0);
                if (passes >= 4)
                {
                    cmd.Blit(temps[0], temps[1], material, 0);
                    cmd.Blit(temps[1], temps[0], material, 0);
                    cmd.Blit(temps[0], blurTexture, material, 0);
                }
                else if (passes == 2 || passes == 3)
                    cmd.Blit(temps[0], blurTexture, material, 0);
                cmd.Blit(source, source, material, 1);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            /// <summary>
            /// Cleanup any allocated data that was created during the execution of the pass
            /// </summary>
            /// <param name="cmd"></param>
            public override void FrameCleanup(CommandBuffer cmd)
            {
                for (int i = 0; i < tempID.Length; i++)
                    cmd.ReleaseTemporaryRT(tempID[i]);
                cmd.ReleaseTemporaryRT(tempCopyID);
                cmd.ReleaseTemporaryRT(blurTextureID);
            }
        }

        public static BlurURP Instance { get; private set; }

        public static BlurSettings Settings
        {
            get
            {
                return Instance.settings;
            }
            set
            {
                Instance.settings = value;
            }
        }

        [SerializeField]
        private BlurSettings settings = new BlurSettings();

        /// <summary>
        /// Instance parameters
        /// </summary>
        [System.Serializable]
        public class BlurSettings
        {
            public BlurSettings()
            {
                Instance = this;
            }

            public BlurSettings(BlurSettings settings)
            {
                passEvent = settings.PassEvent;
                maskTexture = settings.MaskTexture;
                intensity = settings.Intensity;
                passes = settings.Passes;
                isActive = settings.IsActive;
                increaseKernelSize = settings.IncreaseKernelSize;
            }

            public static BlurSettings Instance { get; private set; }
            public const float INTENSITY_MIN_VALUE = 0f, INTENSITY_MAX_VALUE = 4f;
            public const int PASSES_MIN_VALUE = 1, PASSES_MAX_VALUE = 5;

            public RenderPassEvent PassEvent
            {
                get
                {
                    return passEvent;
                }
                set
                {
                    passEvent = value;
                    BlurURP.Instance.Create();
                }
            }

            public bool IsActive
            {
                get
                {
                    return isActive;
                }
                set
                {
                    isActive = value;
                    BlurURP.Instance.Create();
                }
            }

            public int Passes
            {
                get
                {
                    return passes;
                }
                set
                {
                    passes = Mathf.Clamp(value, PASSES_MIN_VALUE, PASSES_MAX_VALUE);
                    BlurURP.Instance.Create();
                }
            }

            public float Intensity
            {
                get
                {
                    return intensity;
                }
                set
                {
                    intensity = Mathf.Clamp(value, INTENSITY_MIN_VALUE, INTENSITY_MAX_VALUE);
                    BlurURP.Instance.Create();
                }
            }

            public bool IncreaseKernelSize
            {
                get
                {
                    return increaseKernelSize;
                }
                set
                {
                    increaseKernelSize = value;
                    BlurURP.Instance.Create();
                }
            }

            public Texture2D MaskTexture
            {
                get
                {
                    return maskTexture;
                }
                set
                {
                    maskTexture = value;
                    BlurURP.Instance.Create();
                }
            }

            public Material Material
            {
                get
                {
                    return material;
                }
            }

            [Tooltip("Controls when the render pass executes")]
            [SerializeField]
            private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

            [Tooltip("Activating/deactivating blurring on the scene")]
            [SerializeField]
            private bool isActive = true;

            [Tooltip("The higher the value, the more blurry the image will be")]
            [SerializeField]
            [Range(PASSES_MIN_VALUE, PASSES_MAX_VALUE)]
            private int passes = 3;

            [Tooltip("Blur level")]
            [SerializeField]
            [Range(INTENSITY_MIN_VALUE, INTENSITY_MAX_VALUE)]
            private float intensity = 1f;

            [Tooltip("When using the parameter, the blur quality increases, but performance suffers")]
            [SerializeField]
            private bool increaseKernelSize = false;

            [Tooltip("The lighter the mask area, the more intense the blur. Blurring is not applied to black areas, which means the shader works faster")]
            [SerializeField]
            private Texture2D maskTexture = null;

            [Tooltip("Blur material. Use the one that is included in the asset")]
            [SerializeField]
            private Material material = null;
        }

        private BlurPass blurPass;
        private GameObject blurController;
        private BlurSettings startSettings;

        /// <summary>
        /// Creating a blur pass
        /// </summary>
        public override void Create()
        {
            if (Instance == null)
                Instance = this;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                startSettings = new BlurSettings(Settings);
            else if (blurController == null)
            {
                blurController = new GameObject("[BlurController]");
                blurController.AddComponent<BlurController>().Init(startSettings, Settings);
                DontDestroyOnLoad(blurController);
            }
#endif
            blurPass = new BlurPass(settings.PassEvent, name, settings.Intensity, settings.IncreaseKernelSize, settings.MaskTexture, settings.Passes, settings.IsActive, settings.Material);
        }

        /// <summary>
        /// Adding a rendering pass for a blur pass
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="renderingData"></param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if !UNITY_2022_1_OR_NEWER
            blurPass.Setup(renderer.cameraColorTarget);
#endif
            renderer.EnqueuePass(blurPass);
        }

#if UNITY_2022_1_OR_NEWER
        /// <summary>
        /// Contains functionality from AddRenderPasses asset of previous versions of Unity to eliminate the bug with the display of blurring
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="renderingData"></param>
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            blurPass.Setup(renderer.cameraColorTargetHandle);
        }
#endif
    }

    [HelpURL("https://assetstore.unity.com/packages/slug/242848")]
    public class BlurController : MonoBehaviour
    {
        private RenderPassEvent startPassEvent;
        private Texture2D startMaskTexture;
        private float startIntensity;
        private int startPasses;
        private bool startIsActive, startIncreaseKernelSize;

        private BlurSettings settings;

        /// <summary>
        /// Saving initial parameter values
        /// </summary>
        public void Init(BlurSettings startSettings, BlurSettings settings)
        {
            startPassEvent = startSettings.PassEvent;
            startMaskTexture = startSettings.MaskTexture;
            startIntensity = startSettings.Intensity;
            startPasses = startSettings.Passes;
            startIsActive = startSettings.IsActive;
            startIncreaseKernelSize = startSettings.IncreaseKernelSize;
            this.settings = settings;
        }

        /// <summary>
        /// Restoring parameter values when exiting the play mode
        /// </summary>
        private void OnApplicationQuit()
        {
            settings.PassEvent = startPassEvent;
            settings.MaskTexture = startMaskTexture;
            settings.Intensity = startIntensity;
            settings.Passes = startPasses;
            settings.IsActive = startIsActive;
            settings.IncreaseKernelSize = startIncreaseKernelSize;
        }
    }
}