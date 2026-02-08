// encoding: utf-8
// Lilliput URP renderer feature

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LilliputRendererFeature : ScriptableRendererFeature
{
    [Serializable]
    public class LilliputSettings
    {
        public Shader shader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public bool enableInSceneView = true;
    }

    [SerializeField]
    private LilliputSettings settings = new LilliputSettings();

    private LilliputRenderPass pass;

    public override void Create()
    {
        if (settings.shader == null)
        {
            settings.shader = Shader.Find("Hidden/LilliputURP");
        }

        if (settings.shader == null)
        {
            Debug.LogError("LilliputRendererFeature: Shader Hidden/LilliputURP not found.");
            return;
        }

        pass = new LilliputRenderPass(settings.shader)
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

    private class LilliputRenderPass : ScriptableRenderPass
    {
        private static readonly int CoCTexId = Shader.PropertyToID("_CoCTex");
        private static readonly int DoFTexId = Shader.PropertyToID("_DoFTex");
        private static readonly int FocusDistanceId = Shader.PropertyToID("_FocusDistance");
        private static readonly int FocusRangeId = Shader.PropertyToID("_FocusRange");
        private static readonly int BlurStrengthId = Shader.PropertyToID("_BokehRadius");
        private static readonly int GaussianStrengthId = Shader.PropertyToID("_GaussianStrength");

        private const int CircleOfConfusionPass = 0;
        private const int PreFilterPass = 1;
        private const int BokehPass = 2;
        private const int PostFilterPass = 3;
        private const int CombinePass = 4;
        private const int GaussianHorizontalPass = 5;
        private const int GaussianVerticalPass = 6;

        private Material material;
        private RTHandle cocHandle;
        private RTHandle dof0Handle;
        private RTHandle dof1Handle;
        private RTHandle colorCopyHandle;
        private RTHandle gaussianTempHandle;
        private RTHandle source;

        public bool IsMaterialValid => material != null;

        public LilliputRenderPass(Shader shader)
        {
            profilingSampler = new ProfilingSampler("Lilliput");
            if (shader != null)
            {
                material = CoreUtils.CreateEngineMaterial(shader);
            }
            // 请求深度纹理
            ConfigureInput(ScriptableRenderPassInput.Depth);
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
                name: "_LilliputColorCopy");

            RenderingUtils.ReAllocateIfNeeded(
                ref gaussianTempHandle,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_LilliputGaussianTemp");

            var cocDescriptor = descriptor;
            cocDescriptor.graphicsFormat = GraphicsFormat.R16_SFloat;
            RenderingUtils.ReAllocateIfNeeded(
                ref cocHandle,
                cocDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_LilliputCoC");

            var halfDescriptor = descriptor;
            halfDescriptor.width = Mathf.Max(1, descriptor.width / 2);
            halfDescriptor.height = Mathf.Max(1, descriptor.height / 2);
            RenderingUtils.ReAllocateIfNeeded(
                ref dof0Handle,
                halfDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_LilliputDoF0");
            RenderingUtils.ReAllocateIfNeeded(
                ref dof1Handle,
                halfDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_LilliputDoF1");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null)
            {
                return;
            }

            var volume = VolumeManager.instance.stack.GetComponent<LilliputVolume>();
            if (volume == null || !volume.IsActive())
            {
                return;
            }

            float focusDistance = volume.focusDistance.value;

            // 如果有动态焦点距离，使用动态值
            if (LilliputController.DynamicFocusDistance.HasValue)
            {
                focusDistance = LilliputController.DynamicFocusDistance.Value;
            }

            float focusRange = Mathf.Max(0.1f, volume.focusRange.value);
            float blurStrength = Mathf.Max(0f, volume.blurStrength.value);
            int iterations = volume.blurIterations.value;
            float gaussianStrength = volume.gaussianStrength.value;

            material.SetFloat(FocusDistanceId, focusDistance);
            material.SetFloat(FocusRangeId, focusRange);
            material.SetFloat(BlurStrengthId, blurStrength);
            material.SetFloat(GaussianStrengthId, gaussianStrength);

            var cmd = CommandBufferPool.Get("Lilliput");
            using (new ProfilingScope(cmd, profilingSampler))
            {
                // Pass 0: Generate Circle of Confusion texture
                cmd.Blit(source, cocHandle, material, CircleOfConfusionPass);
                material.SetTexture(CoCTexId, cocHandle);

                // Pass 1: PreFilter - downsample with CoC
                cmd.Blit(source, dof0Handle, material, PreFilterPass);

                // Pass 2 & 3: Bokeh blur with iterations
                for (int i = 0; i < iterations; i++)
                {
                    // Bokeh blur
                    cmd.Blit(dof0Handle, dof1Handle, material, BokehPass);

                    // PostFilter - tent filter smoothing
                    cmd.Blit(dof1Handle, dof0Handle, material, PostFilterPass);
                }

                // Pass 4: Combine - blend sharp and blurred
                material.SetTexture(DoFTexId, dof0Handle);
                cmd.Blit(source, colorCopyHandle);
                cmd.Blit(colorCopyHandle, source, material, CombinePass);

                // Pass 5 & 6: Optional Gaussian blur for extra smoothness
                if (gaussianStrength > 0.01f)
                {
                    cmd.Blit(source, gaussianTempHandle, material, GaussianHorizontalPass);
                    cmd.Blit(gaussianTempHandle, source, material, GaussianVerticalPass);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(material);
            cocHandle?.Release();
            dof0Handle?.Release();
            dof1Handle?.Release();
            colorCopyHandle?.Release();
            gaussianTempHandle?.Release();
        }
    }
}

