sampler uImage0 : register(s0);

float uTime;

float4 FirePS(float2 texCoord : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 uv = texCoord;

    // Sampling uv.x - offset makes the image travel from left to right.
    float flow = frac(uTime * 0.22);
    float wave = sin(uv.x * 6.0 + uTime * 1.5) * 0.012;
    float2 sampleUv = float2(uv.x - flow, uv.y + wave);

    float4 sampleColor = tex2D(uImage0, sampleUv);
    return sampleColor * vertexColor;
}

technique SpriteFire
{
    pass FirePass
    {
        PixelShader = compile ps_3_0 FirePS();
    }
}
