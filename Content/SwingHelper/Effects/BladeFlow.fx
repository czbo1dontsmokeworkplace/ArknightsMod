// 刀光内流动 Shader — 能量沿刀身流动
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
    float4 c = tex2D(uImage0, input.UV);

    // U 沿刀身方向，条纹随 U 移动
    float flow = sin(input.UV.x * 20.0 - uTime * 4.0) * 0.5 + 0.5;
    flow = pow(flow, 3.0);

    c.rgb += float3(0.3, 0.7, 1.0) * flow * 0.8;
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
