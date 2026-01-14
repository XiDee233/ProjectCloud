using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class NearFieldDarkeningSettings
{
    [Tooltip("开始变暗的距离")]
    public float darkeningStartDistance = 10.0f;
    
    [Tooltip("完全变暗的距离")]
    public float darkeningEndDistance = 1.0f;
    
    [Tooltip("变暗强度 (0-1)")]
    [Range(0, 1)]
    public float darkeningIntensity = 1.0f;
}

public class NearFieldDarkeningPostProcess : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public NearFieldDarkeningSettings nearFieldDarkeningSettings = new NearFieldDarkeningSettings();
    }

    public Settings settings = new Settings();
    private NearFieldDarkeningPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new NearFieldDarkeningPass(settings.renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        // 只在主相机上应用
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // 更新设置
        m_ScriptablePass.UpdateSettings(settings.nearFieldDarkeningSettings);
        
        // 设置需要深度纹理
        renderer.EnqueuePass(m_ScriptablePass);
    }

    private class NearFieldDarkeningPass : ScriptableRenderPass
    {
        private Material m_Material;
        private string m_ProfilerTag = "NearFieldDarkening";
        private RenderTargetIdentifier m_ColorTarget;
        private NearFieldDarkeningSettings m_Settings = new NearFieldDarkeningSettings();
        
        public void UpdateSettings(NearFieldDarkeningSettings settings)
        {
            m_Settings = settings;
        }

        public NearFieldDarkeningPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            // 加载着色器
            Shader shader = Shader.Find("URP/NearFieldDarkeningPostProcess");
            if (shader != null)
            {
                m_Material = new Material(shader);
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // 配置需要深度纹理
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogError("NearFieldDarkeningPostProcess: Material not found!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            // 创建临时渲染目标
            int tempRT = Shader.PropertyToID("_NearFieldDarkeningTempRT");
            cmd.GetTemporaryRT(tempRT, descriptor, FilterMode.Bilinear);

            // 设置着色器参数
            m_Material.SetFloat("_DarkeningStartDistance", m_Settings.darkeningStartDistance);
            m_Material.SetFloat("_DarkeningEndDistance", m_Settings.darkeningEndDistance);
            m_Material.SetFloat("_DarkeningIntensity", m_Settings.darkeningIntensity);

            // 应用后处理效果到临时渲染目标
            cmd.Blit(renderingData.cameraData.renderer.cameraColorTarget, tempRT, m_Material, 0);
            // 将结果复制回原目标
            cmd.Blit(tempRT, renderingData.cameraData.renderer.cameraColorTarget);

            // 释放临时渲染目标
            cmd.ReleaseTemporaryRT(tempRT);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }
}