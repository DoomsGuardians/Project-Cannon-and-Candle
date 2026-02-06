using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Ensures the depth texture is enabled for the camera.
/// Required for stylized water shader to work properly.
/// </summary>
[ExecuteAlways]
public class EnableDepthBuffer : MonoBehaviour
{
    private void OnEnable()
    {
        EnableDepthTexture();
    }

    private void OnValidate()
    {
        EnableDepthTexture();
    }

    private void EnableDepthTexture()
    {
        // Try to enable depth texture on the URP asset
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.supportsCameraDepthTexture = true;
            urpAsset.supportsCameraOpaqueTexture = true;
        }

        // Also try to enable on the camera's additional data
        var camera = GetComponent<Camera>();
        if (camera != null)
        {
            var additionalCameraData = camera.GetUniversalAdditionalCameraData();
            if (additionalCameraData != null)
            {
                additionalCameraData.requiresDepthTexture = true;
                additionalCameraData.requiresColorTexture = true;
            }
        }
    }
}
