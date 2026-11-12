// Original pressure-front material and local screen refraction. Existing grayscale noise only.
float uAge, uLife, uRadius, uExtent, uWidth, uPower;
float3 uColor, uCore;
float uGapAngle, uGapCount, uGapHalfWidth;
sampler uNoise : register(s1);
float GapDistance(float2 p)
{
    float angle = atan2(p.y, p.x) - uGapAngle;
    return abs(atan2(sin(angle * uGapCount), cos(angle * uGapCount))) / max(1, uGapCount);
}
float GapMask(float2 p)
{
    return uGapCount < .5 ? 1 : smoothstep(uGapHalfWidth, uGapHalfWidth + .018, GapDistance(p));
}
float4 PressurePixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = (uv * 2 - 1) * uExtent;
    float r = length(p);
    float2 dir = p / max(r, 1);
    float n = tex2D(uNoise, dir * 1.8 + float2(uAge * .006, r * .0017 - uAge * .014)).r;
    float fine = tex2D(uNoise, p / 190 - dir * uAge * .016).r;
    float d = r - uRadius + (n - .5) * uWidth * .24;
    float front = exp(-pow(d / (uWidth * .38), 2));
    float crest = exp(-pow((d + uWidth * .06) / max(2, uWidth * .065), 2));
    float wake = exp(-pow((d + uWidth * .9) / (uWidth * 1.6), 2));
    float inner = smoothstep(-uWidth * 3.5, -uWidth * .6, d);
    float angle = atan2(dir.y, dir.x);
    float spokes = pow(saturate(sin(angle * 53 + sin(angle * 13) * 3)), 18) * .42;
    float rays = spokes * inner * (1 - smoothstep(-uWidth * .4, uWidth * .3, d));
    float fade = (1 - smoothstep(.52, 1, uLife)) * (1.05 - uLife * .45);
    float3 rgb = uColor * (front * .85 + wake * (.32 + fine * .22) + rays * .45)
        + uCore * (crest * .65 + front * .24);
    // A narrow dark compression lip makes a luminous front legible on bright terrain too.
    float lip = exp(-pow((d - uWidth * .9) / (uWidth * .32), 2)) * .16;
    float bounds = 1 - smoothstep(uExtent * .93, uExtent, r);
    float gap = GapMask(p);
    return float4(rgb * fade * uPower * bounds * gap, lip * fade * bounds * gap);
}

float uWarningProgress, uWarningOpacity;
float4 WarningPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = (uv * 2 - 1) * uExtent;
    float r = length(p), angle = GapDistance(p);
    float safe = 1 - smoothstep(uGapHalfWidth - .012, uGapHalfWidth, angle);
    float boundary = exp(-pow((angle - uGapHalfWidth) / .009, 2));
    float center = exp(-pow(angle / .045, 2));
    float flow = pow(saturate(sin(r / 48 - uAge * .22)), 8);
    float falloff = smoothstep(45, 105, r) * (1 - smoothstep(uExtent - 110, uExtent - 48, r));
    float3 rgb = uColor * safe * (.065 + uWarningProgress * .035 + center * flow * .11)
        + uCore * boundary * (.38 + uWarningProgress * .16);
    return float4(rgb * falloff * uWarningOpacity, 0);
}

float2 uScreenResolution, uScreenPosition, uTargetPosition;
float uOpacity, uWaveRadius, uWaveWidth, uWaveStrength;
sampler uScreen : register(s0);
float4 PressureRefraction(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * uScreenResolution + uScreenPosition - uTargetPosition;
    float r = length(p), d = (r - uWaveRadius) / max(1, uWaveWidth);
    float displacement = d * exp(-d*d*2) * uWaveStrength * uOpacity * GapMask(p);
    float2 offset = p / max(r, 1) * displacement / uScreenResolution;
    return tex2D(uScreen, saturate(uv + offset)) * tint;
}
technique PressureFront
{
    pass FrontPass { PixelShader = compile ps_3_0 PressurePixel(); }
}
technique PressureWarp
{
    pass WarpPass { PixelShader = compile ps_3_0 PressureRefraction(); }
}
technique PressureWarning
{
    pass WarningPass { PixelShader = compile ps_3_0 WarningPixel(); }
}
