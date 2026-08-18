// 刀光消融 Shader — 噪声贴图版"水晒干"
// uNoiseTex：灰度噪声图（四方连续、大斑块），大尺度采样形成成块蒸发
// uDissolve：0 = 完整，1 = 完全消融
// 结构照抄 LupineKnifeLight.fx（SV_POSITION + Color 直通）
sampler uImage0 : register(s0);
sampler uNoiseTex : register(s1);
float4x4 uTransform;
float uTime;
float uDissolve;
float uNoiseScale;   // 噪声采样尺度（1.5~3：越大斑块越大）

struct VSInput
{
    float2 Pos : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VS(VSInput input)
{
    VSOutput o;
    o.Pos = mul(float4(input.Pos, 0, 1), uTransform);
    o.Color = input.Color;
    o.TexCoord = input.TexCoord;
    return o;
}

float4 PS(VSOutput input) : COLOR0
{
    float4 c = tex2D(uImage0, input.TexCoord);
    float2 uv = input.TexCoord;

    // 大尺度采样噪声（大斑块），随时间极缓慢漂移
    float field = tex2D(uNoiseTex, uv * uNoiseScale + uTime * 0.01).r;
    // 第二层稍小尺度，给斑块内部结构
    float detail = tex2D(uNoiseTex, uv * uNoiseScale * 3.0 + 0.37).r;
    field = field * 0.75 + detail * 0.25;

    // 阈值：field 低于阈值的整块蒸发
    float alive = smoothstep(uDissolve, uDissolve + 0.15, field);
    c.rgb *= alive;

    // 干涸边缘：剩余斑块边缘的水痕亮边
    float edge = alive * (1.0 - smoothstep(uDissolve + 0.03, uDissolve + 0.3, field));
    c.rgb += float3(0.6, 0.9, 1.2) * edge * 0.5;

    c.a = 1.0;
    return c * input.Color;
}

technique Technique1
{
    pass Base
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}
