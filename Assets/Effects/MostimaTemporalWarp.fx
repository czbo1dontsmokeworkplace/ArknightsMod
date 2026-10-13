sampler BaseTexture : register(s0);
float uTime;
float uTwist;

float4 TemporalWarp(float2 uv : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float2 delta = uv - float2(0.5, 0.5);
    float radius = length(delta);
    float influence = saturate(1.0 - radius * 1.65);
    float twist = uTwist * influence * influence + 0.055 * sin(uTime + radius * 19.0) * influence;
    float s = sin(twist);
    float c = cos(twist);
    float2 rotated = float2(delta.x * c - delta.y * s, delta.x * s + delta.y * c);
    float suction = 1.0 - 0.13 * influence;
    float2 sampleUv = float2(0.5, 0.5) + rotated * suction;
    return tex2D(BaseTexture, sampleUv) * tint;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 TemporalWarp();
    }
}
