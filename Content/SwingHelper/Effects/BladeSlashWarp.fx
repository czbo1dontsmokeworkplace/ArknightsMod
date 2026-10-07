// Screen distortion limited to the current blade segment.
sampler uImage0 : register(s0);
float uTime;
float uOpacity;
float2 uStart;
float2 uEnd;
float uAngle;
float uWidth;
float uStrength;
float uChromatic;

float4 PS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float2 uv = texCoord;
    float4 original = tex2D(uImage0, uv);
    float2 segment = uEnd - uStart;
    float segmentLength = dot(segment, segment);
    float mask = 0.0;
    if (segmentLength > 0.000001)
    {
        float t = dot(uv - uStart, segment) / segmentLength;
        t = max(t, 0.0);
        t = min(t, 1.0);
        float2 closest = uStart + segment * t;
        float distanceToBlade = length(uv - closest);
        mask = 1.0 - smoothstep(uWidth, uWidth * 2.0, distanceToBlade);
    }

    float2 offsetDirection = float2(cos(uAngle), sin(uAngle));
    float wave = sin(dot(uv, float2(180.0, 96.0)) - uTime * 8.0) * 0.5 + 0.5;
    float2 offset = offsetDirection * uStrength * mask * (0.55 + wave * 0.45);
    float2 chromatic = offsetDirection * uChromatic * mask;
    float red = tex2D(uImage0, uv + offset + chromatic).r;
    float green = tex2D(uImage0, uv + offset).g;
    float blue = tex2D(uImage0, uv + offset - chromatic).b;
    float4 modified = float4(red, green, blue, original.a);
    return lerp(original, modified, mask * uOpacity) * color;
}

technique SlashWarp
{
    pass SlashWarp
    {
        PixelShader = compile ps_3_0 PS();
    }
}
