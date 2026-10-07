sampler uImage0 : register(s0);
float uTime;
float uOpacity;
float2 uCenter;
float uRadius;
float uStrength;

float4 PS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float2 uv = texCoord;
    float4 original = tex2D(uImage0, uv);

    // 到刀剑中心的距离与方向
    float2 dir = uv - uCenter;
    float dist = length(dir);

    // 影响遮罩：半径内 = 1，半径外 = 0（唯一的 smoothstep，其余区域完全不动）
    float mask = 1.0 - smoothstep(uRadius * 0.3, uRadius, dist);

    // 吸入：采样点沿径向向外偏移 → 视觉上内容朝刀剑聚拢（速度拉扯感）
    float2 dirN = dir / max(dist, 0.0001);
    float2 sampleUV = uv + dirN * uStrength * mask;

    // 色散：红蓝通道沿径向反向错位，吸入边缘带色边（速度残影感）
    float chroma = uStrength * 0.5 * mask;
    float r = tex2D(uImage0, sampleUV + dirN * chroma).r;
    float g = tex2D(uImage0, sampleUV).g;
    float b = tex2D(uImage0, sampleUV - dirN * chroma).b;
    float4 modified = float4(r, g, b, 1.0);

    return lerp(original, modified, uOpacity);
}

technique WarpTechnique
{
    pass SpeedWarp
    {
        PixelShader = compile ps_3_0 PS();
    }
}
