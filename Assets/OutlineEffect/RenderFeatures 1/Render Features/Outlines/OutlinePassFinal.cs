using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Xenon
{
    public class OutlinePassFinal : ScriptableRenderPass
    {
        private class PassData
        {
            internal TextureHandle FilterTextureHandle;
            internal TextureHandle OpaqueTextureHandle;
            internal Material Material;
        }

        private static readonly int FilterTexture = Shader.PropertyToID("_FilterTexture");
        private static readonly int OutlineScale = Shader.PropertyToID("_OutlineScale");
        private static readonly int RobertsCrossMultiplier = Shader.PropertyToID("_RobertsCrossMultiplier");
        private static readonly int DepthThreshold = Shader.PropertyToID("_DepthThreshold");
        private static readonly int NormalThreshold = Shader.PropertyToID("_NormalThreshold");
        private static readonly int SteepAngleThreshold = Shader.PropertyToID("_SteepAngleThreshold");
        private static readonly int SteepAngleMultiplier = Shader.PropertyToID("_SteepAngleMultiplier");
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int UseNormalOutline = Shader.PropertyToID("_UseNormalOutline");

        private readonly Material _blitMaterial;
        private readonly OutlineRenderFeature.Settings _featureSettings;
        private readonly OutlineRenderFeature.OutlineSettings _outlineSettings;

        public OutlinePassFinal(OutlineRenderFeature.Settings featureSettings, OutlineRenderFeature.OutlineSettings outlineSettings)
        {
            this.renderPassEvent = featureSettings.RenderPassEvent;
            this._featureSettings = featureSettings;
            this._outlineSettings = outlineSettings;

            _blitMaterial = CoreUtils.CreateEngineMaterial("Hidden/S_ScreenSpaceOutlines");
            UpdateMaterialProperties();
        }

        private void UpdateMaterialProperties()
        {
            if (_blitMaterial != null)
            {
                _blitMaterial.SetFloat("_OutlineScale", _outlineSettings.OutlineScale);
                _blitMaterial.SetFloat("_RobertsCrossMultiplier", _outlineSettings.RobertsCrossMultiplier);
                _blitMaterial.SetFloat("_DepthThreshold", _outlineSettings.DepthThreshold);
                _blitMaterial.SetFloat("_NormalThreshold", _outlineSettings.NormalThreshold);
                _blitMaterial.SetFloat("_SteepAngleThreshold", _outlineSettings.SteepAngleThreshold);
                _blitMaterial.SetFloat("_SteepAngleMultiplier", _outlineSettings.SteepAngleMultiplier);
                _blitMaterial.SetColor("_OutlineColor", _outlineSettings.OutlineColor);
                _blitMaterial.SetFloat("_UseNormalOutline", 0.0f);
            }
        }

        private static void ExecutePass(PassData passData, RasterGraphContext context)
        {
            if (passData.Material != null)
            {
                passData.Material.SetTexture(FilterTexture, passData.FilterTextureHandle);
            }

            Blitter.BlitTexture(context.cmd, passData.FilterTextureHandle, new Vector4(1, 1, 0, 0), passData.Material, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var outlineData = frameData.Get<OutlineRenderFeature.OutlineData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData>("OutlinePass_Final", out var passData, new ProfilingSampler("OutlinePass_Final"));

            if (!outlineData.FilterTextureHandle.IsValid())
                return;

            if (_blitMaterial == null)
                return;

            passData.Material = _blitMaterial;
            passData.FilterTextureHandle = outlineData.FilterTextureHandle;

            builder.AllowPassCulling(false);
            builder.UseTexture(passData.FilterTextureHandle);
            builder.SetRenderAttachment(resourceData.cameraColor, index: 0);
            builder.SetRenderFunc<PassData>(ExecutePass);
        }

        /*
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            UpdateMaterialProperties(); // Ensure material properties are updated before rendering

            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = _featureSettings.ClearDepth ? 32 : 0;
        }
        */
    }
}