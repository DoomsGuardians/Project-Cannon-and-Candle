// encoding: utf-8
// GT (Gran Turismo) Tonemapping URP renderer feature
// GT 色调映射 - 来自 Gran Turismo 的色调映射曲线

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GTTonemappingRendererFeature : ScriptableRendererFeature
{
    [Serializable]
    public class GTTonemappingSettings
    {
        public Shader shader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public bool enableInSceneView = true;
    }

    [SerializeField]
    private GTTonemappingSettings settings = new GTTonemappingSettings();

    private GTTonemappingRenderPass pass;

    public override void Create()
    {
        if (settings.shader == null)
        {
            settings.shader = Shader.Find("Hidden/PostProcessing/GTTonemapping");
        }

        if (settings.shader == null)
        {
            Debug.LogError("GTTonemappingRendererFeature: Shader Hidden/PostProcessing/GTTonemapping not found.");
            return;
        }

        pass = new GTTonemappingRenderPass(settings.shader)
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

    private class GTTonemappingRenderPass : ScriptableRenderPass
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int MaxBrightnessId = Shader.PropertyToID("_MaxBrightness");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int LinearStartId = Shader.PropertyToID("_LinearStart");
        private static readonly int LinearLengthId = Shader.PropertyToID("_LinearLength");
        private static readonly int ToeStrengthId = Shader.PropertyToID("_ToeStrength");
        private static readonly int BlackTightenId = Shader.PropertyToID("_BlackTighten");

        private Material material;
        private RTHandle colorCopyHandle;
        private RTHandle source;

        public bool IsMaterialValid => material != null;

        public GTTonemappingRenderPass(Shader shader)
        {
            profilingSampler = new ProfilingSampler("GT Tonemapping");
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
                name: "_GTTonemappingColorCopy");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null)
            {
                return;
            }

            var volume = VolumeManager.instance.stack.GetComponent<GTTonemappingVolume>();
            if (volume == null || !volume.IsActive())
            {
                return;
            }

            material.SetFloat(IntensityId, volume.intensity.value);
            material.SetFloat(MaxBrightnessId, volume.maxBrightness.value);
            material.SetFloat(ContrastId, volume.contrast.value);
            material.SetFloat(LinearStartId, volume.linearStart.value);
            material.SetFloat(LinearLengthId, volume.linearLength.value);
            material.SetFloat(ToeStrengthId, volume.toeStrength.value);
            material.SetFloat(BlackTightenId, volume.blackTighten.value);

            var cmd = CommandBufferPool.Get("GT Tonemapping");
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
