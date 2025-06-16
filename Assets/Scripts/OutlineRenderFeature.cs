using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        public Material outlineMaterial = null;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public OutlineSettings settings = new OutlineSettings();
    private OutlineRenderPass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlineRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null)
            return;

        renderer.EnqueuePass(outlinePass);
    }
}

public class OutlineRenderPass : ScriptableRenderPass
{
    private OutlineRenderFeature.OutlineSettings settings;
    private RTHandle source;
    private RTHandle tempTexture;
    private string profilerTag = "Outline Pass";

    public OutlineRenderPass(OutlineRenderFeature.OutlineSettings settings)
    {
        this.settings = settings;
        this.renderPassEvent = settings.renderPassEvent;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, name: "_TempOutlineTexture");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

        Blitter.BlitCameraTexture(cmd, source, tempTexture, settings.outlineMaterial, 0);
        Blitter.BlitCameraTexture(cmd, tempTexture, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (tempTexture != null)
        {
            tempTexture.Release();
            tempTexture = null;
        }
    }
}