using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class OutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        public Shader outlineShader;

        [Range(1, 4)]
        public int scale = 1;

        public Color color = Color.white;
        public Color normalColor = Color.white;

        [Range(0.0f, 10.0f)]
        public float depthThreshold = 1.5f;

        [Range(0, 1)]
        public float depthNormalThreshold = 0.5f;

        [Range(1, 20)]
        public float depthNormalThresholdScale = 7;

        [Range(0, 1)]
        public float normalThreshold = 0.4f;
    }

    public OutlineSettings settings = new OutlineSettings();
    private OutlinePass outlinePass;

    public override void Create()
    {
        if (settings.outlineShader == null)
        {
            settings.outlineShader = Shader.Find("Hidden/OutlineURP");
        }
        outlinePass = new OutlinePass(settings);
        outlinePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlinePass == null || settings.outlineShader == null) return;

        renderer.EnqueuePass(outlinePass);
    }

    class OutlinePass : ScriptableRenderPass
    {
        private readonly OutlineSettings settings;
        private Material material;

        public OutlinePass(OutlineSettings settings)
        {
            this.settings = settings;
            if (settings.outlineShader != null)
            {
                this.material = new Material(settings.outlineShader);
            }
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Outline Pass");

            material.SetInt("_Scale", settings.scale);
            material.SetColor("_Color", settings.color);
            material.SetColor("_NormalColor", settings.normalColor);
            material.SetFloat("_DepthThreshold", settings.depthThreshold);
            material.SetFloat("_DepthNormalThreshold", settings.depthNormalThreshold);
            material.SetFloat("_DepthNormalThresholdScale", settings.depthNormalThresholdScale);
            material.SetFloat("_NormalThreshold", settings.normalThreshold);

            var camera = renderingData.cameraData.camera;
            Matrix4x4 clipToView = camera.projectionMatrix.inverse;
            material.SetMatrix("_ClipToView", clipToView);

            var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blit(cmd, cameraColorTarget, cameraColorTarget, material, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}