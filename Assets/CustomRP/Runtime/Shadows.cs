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

    private static int dirShadowMapId = Shader.PropertyToID("_DirectionalShadowMap");
    private static int dirShadowMatrix = Shader.PropertyToID("_DirectionalShadowMatrix");

    public void SetupDirectionalShadow()
    {
        int size = (int)settings.directional.mapSize;
        buffer.GetTemporaryRT(dirShadowMapId, size, size, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowMapId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);

        Matrix4x4 viewM, projM;
        ShadowSplitData splitData;
        var shadowSettings = new ShadowDrawingSettings(cullingResults, 0);
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(0, 0, 1, Vector3.zero, size, 0,
            out viewM, out projM, out splitData);
        shadowSettings.splitData = splitData;
        buffer.SetGlobalMatrix(dirShadowMatrix, projM * viewM);
        buffer.SetViewProjectionMatrices(viewM, projM);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowMapId);
    }
}
