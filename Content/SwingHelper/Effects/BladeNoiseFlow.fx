// 刀光不规则流动 Shader — 噪声扰动 + 波动
sampler uImage0 : register(s0);
float4x4 uTransform;
float uTime;

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
    // 噪声扰动 UV
    float2 uv = input.UV;
    float noise = frac(sin(dot(input.UV * 10.0 + uTime * 0.5, float2(12.9898, 78.233))) * 43758.5453);
    uv.x += (noise - 0.5) * 0.1;

    float4 c = tex2D(uImage0, uv);

    // 竖向波动
    float wave = sin(input.UV.x * 15.0 - uTime * 6.0) * 0.3;
    c.rgb += float3(0.5, 0.3, 1.0) * wave * 0.6;
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
