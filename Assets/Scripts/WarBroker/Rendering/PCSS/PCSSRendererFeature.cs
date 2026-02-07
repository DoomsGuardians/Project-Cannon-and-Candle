using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WarBroker.Rendering
{
    /// <summary>
    /// PCSS屏幕空间阴影渲染特性
    /// 完全替换URP的PCF阴影，生成_ScreenSpaceShadowmapTexture
    /// </summary>
    public class PCSSRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class PCSSSettings
        {
            public Shader shader;

            [Header("PCSS Parameters")]
            [Tooltip("软阴影强度 - 值越大阴影越软")]
            [Range(1f, 500f)]
            public float softness = 200f;

            [Tooltip("遮挡物搜索半径 (UV空间)")]
            [Range(0.01f, 0.2f)]
            public float blockerSearchRadius = 0.05f;

            [Tooltip("最大滤波半径 (UV空间)")]
            [Range(0.01f, 0.3f)]
            public float maxFilterRadius = 0.15f;

            [Tooltip("遮挡物搜索采样数")]
            [Range(8, 64)]
            public int blockerSamples = 16;

            [Tooltip("PCF滤波采样数")]
            [Range(16, 64)]
            public int filterSamples = 32;
        }

        public PCSSSettings settings = new PCSSSettings();

        private PCSSScreenSpaceShadowPass m_RenderPass;
        private Material m_Material;

        public override void Create()
        {
            if (settings.shader == null)
                settings.shader = Shader.Find("Hidden/PCSS/ScreenSpaceShadows");

            if (settings.shader == null)
            {
                Debug.LogError("PCSS: Shader not found!");
                return;
            }

            m_Material = CoreUtils.CreateEngineMaterial(settings.shader);
            m_RenderPass = new PCSSScreenSpaceShadowPass(m_Material, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Material == null || m_RenderPass == null)
                return;

            if (renderingData.cameraData.cameraType != CameraType.Game &&
                renderingData.cameraData.cameraType != CameraType.SceneView)
                return;

            // 需要主光源阴影支持
            if (!renderingData.shadowData.supportsMainLightShadows)
                return;

            m_RenderPass.Setup(settings);
            renderer.EnqueuePass(m_RenderPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_RenderPass?.Dispose();
            CoreUtils.Destroy(m_Material);
        }

        /// <summary>
        /// PCSS屏幕空间阴影Pass
        /// 在阴影贴图生成后立即执行，生成_ScreenSpaceShadowmapTexture替换URP的PCF阴影
        /// </summary>
        private class PCSSScreenSpaceShadowPass : ScriptableRenderPass
        {
            private Material m_Material;
            private PCSSSettings m_Settings;
            private RTHandle m_ScreenSpaceShadowRT;
            private int m_FrameIndex;

            // Shader属性ID
            private static readonly int s_PCSSParams = Shader.PropertyToID("_PCSSParams");
            private static readonly int s_PCSSParams2 = Shader.PropertyToID("_PCSSParams2");
            private static readonly int s_InverseVP = Shader.PropertyToID("_InverseViewProjectionMatrix");
            private static readonly int s_ScreenSpaceShadowmapTexture = Shader.PropertyToID("_ScreenSpaceShadowmapTexture");
            private static readonly int s_BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");

            // 屏幕空间阴影关键字
            private static readonly GlobalKeyword s_MainLightShadowsScreenKeyword = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_SCREEN");
            private static readonly GlobalKeyword s_MainLightShadowsCascadeKeyword = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE");
            private static readonly GlobalKeyword s_MainLightShadowsKeyword = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS");

            public PCSSScreenSpaceShadowPass(Material material, PCSSSettings settings)
            {
                m_Material = material;
                m_Settings = settings;

                // 在CopyDepth之后、不透明物体渲染之前执行
                // 这确保深度纹理已经准备好
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

                // 需要深度纹理
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public void Setup(PCSSSettings settings)
            {
                m_Settings = settings;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // 创建屏幕空间阴影RT
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm;

                RenderingUtils.ReAllocateIfNeeded(ref m_ScreenSpaceShadowRT, desc, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_ScreenSpaceShadowmapTexture");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_Material == null) return;

                CommandBuffer cmd = CommandBufferPool.Get("PCSS Screen Space Shadows");

                var camera = renderingData.cameraData.camera;

                // 设置PCSS参数
                cmd.SetGlobalVector(s_PCSSParams, new Vector4(
                    m_Settings.softness,
                    m_Settings.blockerSearchRadius,
                    m_Settings.maxFilterRadius,
                    0
                ));

                m_FrameIndex = (m_FrameIndex + 1) % 64;
                cmd.SetGlobalVector(s_PCSSParams2, new Vector4(
                    m_Settings.blockerSamples,
                    m_Settings.filterSamples,
                    m_FrameIndex,
                    0
                ));

                // 设置逆VP矩阵用于深度重建世界坐标
                Matrix4x4 view = camera.worldToCameraMatrix;
                Matrix4x4 proj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                cmd.SetGlobalMatrix(s_InverseVP, (proj * view).inverse);

                // 设置Blit参数 (scale=1, bias=0)
                cmd.SetGlobalVector(s_BlitScaleBias, new Vector4(1, 1, 0, 0));

                // 使用全屏三角形渲染
                CoreUtils.SetRenderTarget(cmd, m_ScreenSpaceShadowRT);
                cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Triangles, 3, 1);

                // 设置全局纹理，替换URP的阴影
                cmd.SetGlobalTexture(s_ScreenSpaceShadowmapTexture, m_ScreenSpaceShadowRT);

                // 使用CommandBuffer设置关键字，在渲染命令执行时生效
                cmd.SetKeyword(s_MainLightShadowsScreenKeyword, true);
                cmd.SetKeyword(s_MainLightShadowsCascadeKeyword, false);
                cmd.SetKeyword(s_MainLightShadowsKeyword, false);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // 注意：不在这里禁用关键字，让它保持启用状态直到下一帧
                // URP会在渲染开始时根据设置重新设置关键字
            }

            public void Dispose()
            {
                m_ScreenSpaceShadowRT?.Release();
            }
        }
    }
}
