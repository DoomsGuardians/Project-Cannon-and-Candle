// encoding: utf-8
// Kuwahara Filter URP renderer feature
// 桑原滤镜 - 通过计算四个象限的方差，选择方差最小的象限平均色

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KuwaharaRendererFeature : ScriptableRendererFeature
{
    [Serializable]
    public class KuwaharaSettings
    {
        public Shader shader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public bool enableInSceneView = true;
    }

    [SerializeField]
    private KuwaharaSettings settings = new KuwaharaSettings();

    private KuwaharaRenderPass pass;

    public override void Create()
    {
        if (settings.shader == null)
        {
            settings.shader = Shader.Find("Hidden/PostProcessing/Kuwahara");
        }

        if (settings.shader == null)
        {
            Debug.LogError("KuwaharaRendererFeature: Shader Hidden/PostProcessing/Kuwahara not found.");
            return;
        }

        pass = new KuwaharaRenderPass(settings.shader)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!ShouldRender(in renderingData))
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (pass == null || !pass.IsMaterialValid)
        {
            return;
        }

        if (!ShouldRender(in renderingData))
        {
            return;
        }

        pass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && pass != null)
        {
            pass.Dispose();
        }
    }

    private bool ShouldRender(in RenderingData renderingData)
    {
        if (pass == null || !pass.IsMaterialValid)
        {
            return false;
        }

        if (renderingData.cameraData.isPreviewCamera)
        {
            return false;
        }

        if (!settings.enableInSceneView && renderingData.cameraData.isSceneViewCamera)
        {
            return false;
        }

        if (!renderingData.cameraData.postProcessEnabled)
        {
            return false;
        }

        return true;
    }

    private class KuwaharaRenderPass : ScriptableRenderPass
    {
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");

        private Material material;
        private RTHandle colorCopyHandle;
        private RTHandle source;

        public bool IsMaterialValid => material != null;

        public KuwaharaRenderPass(Shader shader)
        {
            profilingSampler = new ProfilingSampler("Kuwahara");
            if (shader != null)
            {
                material = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        public void Setup(RTHandle sourceHandle)
        {
            source = sourceHandle;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;

            RenderingUtils.ReAllocateIfNeeded(
                ref colorCopyHandle,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_KuwaharaColorCopy");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null)
            {
                return;
            }

            var volume = VolumeManager.instance.stack.GetComponent<KuwaharaVolume>();
            if (volume == null || !volume.IsActive())
            {
                return;
            }

            int radius = volume.radius.value;
            material.SetInt(RadiusId, radius);

            var cmd = CommandBufferPool.Get("Kuwahara");
            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.Blit(source, colorCopyHandle);
                cmd.Blit(colorCopyHandle, source, material, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(material);
            colorCopyHandle?.Release();
        }
    }
}
