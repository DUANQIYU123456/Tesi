// Vefects Distortion - "Distort Transparents" renderer feature (URP 17+, Unity 6 Render Graph)
//
// Why: URP's _CameraOpaqueTexture is captured BEFORE transparent objects are drawn, so any
// transparent surface behind a distortion (lava, water, particles, glass...) is missing from it
// and the distortion shows whatever opaque geometry is underneath instead.
//
// What this does, per camera:
//   1. Before transparents: sets _VefectsDistortionLate = 1, so the normal distortion pass skips itself.
//   2. After transparents:  copies the camera color (now including transparents) into _VefectsSceneColor
//                           and draws every material's "VefectsDistortion" pass, which samples that copy.
//   3. Resets _VefectsDistortionLate = 0.
// If the feature is missing or disabled, the shader falls back to the regular Opaque Texture behaviour.
//
// Setup: select your Universal Renderer Data asset > Add Renderer Feature > Vefects Distortion Feature.
//
// Enjoy using our VFX!
// Love, Ge Lush <3

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Vefects.Distortion
{
    [DisallowMultipleRendererFeature("Vefects Distortion Feature")]
    public class VefectsDistortionFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Layers drawn by the late distortion pass.")]
            public LayerMask layerMask = ~0;

            [Tooltip("Apply the late distortion in the Scene view too.")]
            public bool sceneView = true;
        }

        public Settings settings = new Settings();

        static readonly int s_LateId = Shader.PropertyToID("_VefectsDistortionLate");
        static readonly int s_SceneColorId = Shader.PropertyToID("_VefectsSceneColor");
        ShaderTagId m_ShaderTag; // created in Create(), Unity forbids it in field initializers

        EnablePass m_EnablePass;
        LatePass m_LatePass;

        public override void Create()
        {
            m_ShaderTag = new ShaderTagId("VefectsDistortion");
            m_EnablePass = new EnablePass(this) { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
            m_LatePass = new LatePass(this) { renderPassEvent = RenderPassEvent.AfterRenderingTransparents };
            // We need to read the camera color, so it can't be the backbuffer.
            m_LatePass.requiresIntermediateTexture = true;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_EnablePass);
            renderer.EnqueuePass(m_LatePass);
        }

        bool IsActive(UniversalCameraData cameraData, UniversalResourceData resourceData)
        {
            var type = cameraData.cameraType;
            if (type == CameraType.Preview || type == CameraType.Reflection)
                return false;
            if (type == CameraType.SceneView && !settings.sceneView)
                return false;
            return !resourceData.isActiveTargetBackBuffer;
        }

        // ---------------------------------------------------------------------------------------

        class FlagPassData
        {
            public float value;
        }

        static void AddFlagPass(RenderGraph renderGraph, string name, float value)
        {
            using (var builder = renderGraph.AddUnsafePass<FlagPassData>(name, out var data))
            {
                data.value = value;
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((FlagPassData d, UnsafeGraphContext ctx) =>
                {
                    CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd).SetGlobalFloat(s_LateId, d.value);
                });
            }
        }

        class EnablePass : ScriptableRenderPass
        {
            readonly VefectsDistortionFeature m_Owner;
            public EnablePass(VefectsDistortionFeature owner) { m_Owner = owner; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();
                if (!m_Owner.IsActive(cameraData, resourceData))
                    return;

                AddFlagPass(renderGraph, "Vefects Distortion: Enable Late", 1f);
            }
        }

        class CopyPassData
        {
            public TextureHandle source;
        }

        class DrawPassData
        {
            public RendererListHandle rendererList;
        }

        class LatePass : ScriptableRenderPass
        {
            readonly VefectsDistortionFeature m_Owner;
            public LatePass(VefectsDistortionFeature owner) { m_Owner = owner; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();
                if (!m_Owner.IsActive(cameraData, resourceData))
                    return;

                var renderingData = frameData.Get<UniversalRenderingData>();
                var lightData = frameData.Get<UniversalLightData>();

                // 1) Copy the camera color (opaques + transparents) into _VefectsSceneColor.
                var desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                desc.name = "_VefectsSceneColor";
                desc.clearBuffer = false;
                desc.msaaSamples = MSAASamples.None;
                desc.bindTextureMS = false;
                desc.filterMode = FilterMode.Bilinear;
                desc.wrapMode = TextureWrapMode.Clamp;
                TextureHandle sceneColorCopy = renderGraph.CreateTexture(desc);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Vefects Distortion: Copy Scene Color", out var data))
                {
                    data.source = resourceData.activeColorTexture;
                    builder.UseTexture(data.source, AccessFlags.Read);
                    builder.SetRenderAttachment(sceneColorCopy, 0, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(sceneColorCopy, s_SceneColorId);
                    builder.SetRenderFunc((CopyPassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), 0f, false);
                    });
                }

                // 2) Draw the "VefectsDistortion" pass of every distortion material.
                var drawSettings = RenderingUtils.CreateDrawingSettings(m_Owner.m_ShaderTag, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                var filterSettings = new FilteringSettings(RenderQueueRange.all, m_Owner.settings.layerMask);
                var listParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

                using (var builder = renderGraph.AddRasterRenderPass<DrawPassData>("Vefects Distortion: Draw Late", out var data))
                {
                    data.rendererList = renderGraph.CreateRendererList(listParams);
                    builder.UseRendererList(data.rendererList);
                    builder.UseAllGlobalTextures(true); // _VefectsSceneColor + _CameraDepthTexture (depth fade)
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    builder.SetRenderFunc((DrawPassData d, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.DrawRendererList(d.rendererList);
                    });
                }

                // 3) Back to normal for anything rendered after (other cameras, UI, etc.).
                AddFlagPass(renderGraph, "Vefects Distortion: Disable Late", 0f);
            }
        }
    }
}
