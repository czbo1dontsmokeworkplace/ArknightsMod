// 刀光消融 Shader — 噪声消融 + 边缘蓝光
sampler uImage0 : register(s0);
float4x4 uTransform;
float uTime;
float uDissolve;

struct VSInput
{
    float3 Pos : POSITION0;
    float2 UV  : TEXCOORD0;
};

struct VSOutput
{
    float4 Pos : POSITION0;
    float2 UV  : TEXCOORD0;
};

VSOutput VS(VSInput input)
{
    VSOutput o;
    o.Pos = mul(float4(input.Pos, 1), uTransform);
    o.UV = input.UV;
    return o;
}

float4 PS(VSOutput input) : COLOR0
{
    float4 c = tex2D(uImage0, input.UV);

    // 噪声消融
    float noise = frac(sin(dot(input.UV * 12.0 + uTime * 0.3, float2(12.9898, 78.233))) * 43758.5453);
    c.a *= smoothstep(uDissolve - 0.1, uDissolve, noise);

    // 消融边缘蓝光
    float edge = 1.0 - smoothstep(uDissolve - 0.03, uDissolve + 0.03, noise);
    c.rgb += float3(0.2, 0.6, 1.0) * edge * 0.8;
    return c;
}

technique Technique1
{
    pass Base
    {
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 PS();
    }
}
