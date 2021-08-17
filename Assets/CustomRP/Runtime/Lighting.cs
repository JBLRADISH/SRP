using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const string bufferName = "Lighting";

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private ScriptableRenderContext context;
    private CullingResults cullingResults;

    private static int dirLightWorldDirId = Shader.PropertyToID("_WorldSpaceLightPos0");
    private static int dirLightColorId = Shader.PropertyToID("unity_LightColor0");

    Shadows shadows = new Shadows();
    public void SetUp(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = this.cullingResults;
        buffer.BeginSample(bufferName);
        SetupDirectionalLight();
        shadows.SetUp(context, cullingResults, shadowSettings);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    
    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupDirectionalLight()
    {
        Light light = RenderSettings.sun;
        buffer.SetGlobalVector(dirLightWorldDirId, -light.transform.forward);
        buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
    }

    public void CleanUp()
    {
        shadows.CleanUp();
    }
}
