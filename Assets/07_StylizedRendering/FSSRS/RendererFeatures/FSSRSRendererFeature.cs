using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace FlowState.Rendering
{
    public sealed class FSSRSRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingTransparents;
        [SerializeField] private Shader compositeShader;

        private Material compositeMaterial;
        private FSSRSCompositePass compositePass;

        public override void Create()
        {
            if (compositeShader == null)
                compositeShader = Shader.Find("Hidden/FLOWSTATE/FSSRS Composite");

            CoreUtils.Destroy(compositeMaterial);
            if (compositeShader != null)
                compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);

            compositePass = new FSSRSCompositePass
            {
                renderPassEvent = injectionPoint
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (compositeMaterial == null || compositePass == null || renderingData.cameraData.isPreviewCamera)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Reflection)
                return;

            FSSRSVolumeComponent settings = VolumeManager.instance.stack.GetComponent<FSSRSVolumeComponent>();
            if (settings == null || !settings.IsActive())
                return;

            compositePass.Setup(compositeMaterial, settings);
            renderer.EnqueuePass(compositePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(compositeMaterial);
            compositeMaterial = null;
        }

        private sealed class FSSRSCompositePass : ScriptableRenderPass
        {
            private const string PassName = "FSSRS Print Composite";
            private Material material;

            public FSSRSCompositePass()
            {
                ConfigureInput(ScriptableRenderPassInput.Color |
                    ScriptableRenderPassInput.Depth |
                    ScriptableRenderPassInput.Normal);
            }

            public void Setup(Material targetMaterial, FSSRSVolumeComponent settings)
            {
                material = targetMaterial;
                requiresIntermediateTexture = true;

                material.SetColor(FSSRSShaderIDs.OutlineColor, settings.outlineColor.value);
                material.SetFloat(FSSRSShaderIDs.OutlineIntensity, settings.outlineIntensity.value);
                material.SetFloat(FSSRSShaderIDs.OutlineThickness, settings.outlineThickness.value);
                material.SetFloat(FSSRSShaderIDs.DepthThreshold, settings.depthThreshold.value);
                material.SetFloat(FSSRSShaderIDs.NormalThreshold, settings.normalThreshold.value);
                material.SetFloat(FSSRSShaderIDs.LumaThreshold, settings.lumaThreshold.value);
                material.SetFloat(FSSRSShaderIDs.PosterizeSteps, settings.posterizeSteps.value);
                material.SetFloat(FSSRSShaderIDs.GrainIntensity, settings.grainIntensity.value);
                material.SetFloat(FSSRSShaderIDs.HalftoneIntensity, settings.halftoneIntensity.value);
                material.SetFloat(FSSRSShaderIDs.HalftoneScale, settings.halftoneScale.value);
                material.SetFloat(FSSRSShaderIDs.HatchIntensity, settings.hatchIntensity.value);
                material.SetFloat(FSSRSShaderIDs.PaletteInfluence, settings.paletteInfluence.value);
                material.SetInteger(FSSRSShaderIDs.DebugMode, (int)settings.debugMode.value);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid())
                    return;

                TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "FSSRS Camera Color";
                destinationDescriptor.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);

                RenderGraphUtils.BlitMaterialParameters parameters = new(source, destination, material, 0);
                renderGraph.AddBlitPass(parameters, PassName);
                resourceData.cameraColor = destination;
            }
        }
    }
}
