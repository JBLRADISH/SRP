using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadows";

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private ScriptableRenderContext context;
    private ShadowSettings settings;
    private CullingResults cullingResults;

    public void SetUp(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        settings = shadowSettings;
        SetupDirectionalShadow();
        ExecuteBuffer();
    }
    
    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private static int dirShadowMap = Shader.PropertyToID("_DirectionalShadowMap");
    private static int dirShadowStrength = Shader.PropertyToID("_DirectionalShadowStrength");
    private static int dirShadowMatrix = Shader.PropertyToID("_DirectionalShadowMatrix");

    public void SetupDirectionalShadow()
    {
        int size = (int)settings.directional.mapSize;
        buffer.GetTemporaryRT(dirShadowMap, size, size, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);

        Matrix4x4 viewM, projM;
        ShadowSplitData splitData;
        var shadowSettings = new ShadowDrawingSettings(cullingResults, 0);
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(0, 0, 1, Vector3.zero, size, 0,
            out viewM, out projM, out splitData);
        shadowSettings.splitData = splitData;
        buffer.SetViewProjectionMatrices(viewM, projM);
        if (SystemInfo.usesReversedZBuffer)
        {
            projM.m20 = -projM.m20;
            projM.m21 = -projM.m21;
            projM.m22 = -projM.m22;
            projM.m23 = -projM.m23;
        }
        var scaleOffset = Matrix4x4.identity;
        scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
        scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;
        buffer.SetGlobalMatrix(dirShadowMatrix, scaleOffset * (projM * viewM));
        buffer.SetGlobalFloat(dirShadowStrength, cullingResults.visibleLights[0].light.shadowStrength);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowMap);
    }
}
