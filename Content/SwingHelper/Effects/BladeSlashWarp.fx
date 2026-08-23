// Localized screen distortion along the stored blade-light path.
// uSegments stores up to 16 hand-to-tip sections in normalized screen UV.
sampler uImage0 : register(s0);
float uTime;
float uOpacity;
float4 uSegments[16];
int uSegmentCount;
float uWidth;
float uStrength;
float uChromatic;

float4 PS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float2 uv = texCoord;
    float4 original = tex2D(uImage0, uv);
    float mask = 0.0;
    float2 bestNormal = float2(0.0, 1.0);
    float2 bestTangent = float2(1.0, 0.0);
    float2 bestPoint = uv;
    int fadeDenominator = uSegmentCount - 1;
    if (fadeDenominator < 1)
        fadeDenominator = 1;

    for (int i = 0; i < 16; i++)
    {
        if (i < uSegmentCount)
        {
            float2 start = uSegments[i].xy;
            float2 end = uSegments[i].zw;
            float2 segment = end - start;
            float segmentLength = dot(segment, segment);
            if (segmentLength > 0.000001)
            {
                float t = dot(uv - start, segment) / segmentLength;
                t = max(t, 0.0);
                t = min(t, 1.0);
                float2 closestPoint = start + segment * t;
                float2 delta = uv - closestPoint;
                float distanceToBlade = length(delta);
                float localMask = 1.0 - smoothstep(uWidth, uWidth * 2.0, distanceToBlade);
                float fade = 1.0 - (float)i / (float)fadeDenominator;
                localMask *= fade;

                if (localMask > mask)
                {
                    mask = localMask;
                    bestPoint = closestPoint;
                    bestTangent = normalize(segment);
                    bestNormal = float2(-bestTangent.y, bestTangent.x);
                }
            }
        }
    }

    float along = dot(bestPoint, bestTangent);
    float wave = sin(along * 240.0 - uTime * 7.0) * 0.5 + 0.5;
    float2 offset = bestNormal * uStrength * mask * (0.55 + wave * 0.45);
    float2 chromatic = bestNormal * uChromatic * mask;

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
