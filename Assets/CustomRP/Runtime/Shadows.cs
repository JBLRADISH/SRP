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
    private static Matrix4x4[] dirShadowMatrixArray = new Matrix4x4[4];

    private static Matrix4x4 _scaleOffset = Matrix4x4.identity;
    private static Matrix4x4 ScaleOffset
    {
        get
        {
            if (_scaleOffset == Matrix4x4.identity)
            {
                _scaleOffset.m00 = _scaleOffset.m11 = _scaleOffset.m22 = 0.5f;
                _scaleOffset.m03 = _scaleOffset.m13 = _scaleOffset.m23 = 0.5f;
            }
            return _scaleOffset;
        }
    }

    public void SetupDirectionalShadow()
    {
        int size = (int) settings.directional.mapSize;
        buffer.GetTemporaryRT(dirShadowMap, size, size, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        ExecuteBuffer();

        Matrix4x4 viewM, projM;
        ShadowSplitData splitData;
        int cascadeCount = settings.directional.cascadeCount;
        Vector3 cascadeRatios = settings.directional.CascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
            var shadowSettings = new ShadowDrawingSettings(cullingResults, 0);
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(0, i, cascadeCount, cascadeRatios, size, 0,
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
            dirShadowMatrixArray[i] = ScaleOffset * (projM * viewM);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
        }
        buffer.SetGlobalMatrixArray(dirShadowMatrix, dirShadowMatrixArray);
        buffer.SetGlobalFloat(dirShadowStrength, cullingResults.visibleLights[0].light.shadowStrength);
        ExecuteBuffer();
    }

    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowMap);
    }
}
