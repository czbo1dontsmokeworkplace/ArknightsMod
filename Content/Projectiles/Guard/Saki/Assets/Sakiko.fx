sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float uOpacity;
float alpha;
float4x4 uTransform;

struct VSInput
{
    float3 Pos      : POSITION0;   // fix: float2 ¡ú float3
    float4 Color    : COLOR0;
    float2 Texcoord : TEXCOORD0;   // fix: float3 ¡ú float2
};

struct PSInput
{
    float4 Pos      : SV_POSITION;
    float4 Color    : COLOR0;
    float2 Texcoord : TEXCOORD0;   // fix: float3 ¡ú float2
};

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 coord = input.Texcoord;
    float4 color  = tex2D(uImage0, coord).xyzw;
    float4 color2 = tex2D(uImage1, coord).xyzw;

    float readRed = uOpacity;
    if (color.r < readRed)
    {
        color.rgba = 0;
    }
    if (color2.r == color2.g && color2.g == color2.b)
    {
        color.rgba = 0;
    }
    return float4(color2.xyz, alpha);
}

PSInput VertexShaderFunction(VSInput input)
{
    PSInput output;
    output.Color    = input.Color;
    output.Texcoord = input.Texcoord;
    output.Pos      = mul(float4(input.Pos, 1), uTransform);  // fix: (Pos, 0, 1) ¡ú (Pos, 1)
    return output;
}

technique Technique1
{
    pass Sakiko
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader  = compile ps_3_0 PixelShaderFunction();
    }
}