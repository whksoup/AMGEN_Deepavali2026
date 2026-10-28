using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Amgen.Deepavali
{
    /// <summary>Camera-local post effect. Enqueued by its controller, without changing shared renderer assets.</summary>
    internal sealed class RestorationRenderPass : ScriptableRenderPass, IDisposable
    {
        private readonly Material material;
        private RTHandle copy;

        public RestorationRenderPass(Material material)
        {
            this.material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;
            var source = resources.activeColorTexture;
            var desc = graph.GetTextureDesc(source);
            desc.name = "Deepavali Restoration"; desc.clearBuffer = false;
            var destination = graph.CreateTexture(desc);
            graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0),
                passName: "Deepavali Screen Restoration");
            resources.cameraColor = destination;
        }

        // Retain support for the project's URP compatibility-mode setting as well as Render Graph.
        [Obsolete("Compatibility mode callback")]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0; desc.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref copy, desc, name: "Deepavali Restoration Copy");
        }

        [Obsolete("Compatibility mode callback")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Deepavali Screen Restoration");
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, source, copy, material, 0);
            Blitter.BlitCameraTexture(cmd, copy, source);
            context.ExecuteCommandBuffer(cmd); CommandBufferPool.Release(cmd);
        }

        public void Dispose() { copy?.Release(); copy = null; }
    }
}
