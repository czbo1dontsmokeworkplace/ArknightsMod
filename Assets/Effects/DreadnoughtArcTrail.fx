// Original procedural blade-arc material for ArknightsMod.
// No sampled noise texture or third-party shader is required.
float4x4 ArcTransform;
float3 BodyTint;
float3 ShadowTint;
float3 EdgeTint;
float AnimationTime;

struct ArcInput
{
    float2 Position : POSITION0;
    float4 Tint : COLOR0;
    float2 Surface : TEXCOORD0;
};

struct ArcSurface
{
    float4 Position : POSITION0;
    float4 Tint : COLOR0;
    float2 Surface : TEXCOORD0;
};

ArcSurface ArcVertex(ArcInput v)
{
    ArcSurface o;
    o.Position = mul(float4(v.Position, 0.0, 1.0), ArcTransform);
    o.Tint = v.Tint;
    o.Surface = v.Surface;
    return o;
}

float LatticeSeed(float2 p)
{
    return frac(sin(p.x * 37.7 + p.y * 89.3 + 17.1) * 10939.1641);
}

float SoftField(float2 p)
{
    float2 cell = floor(p);
    float2 local = frac(p);
    float2 blend = local * local * (3.0 - 2.0 * local);
    float bottom = lerp(LatticeSeed(cell), LatticeSeed(cell + float2(1, 0)), blend.x);
    float top = lerp(LatticeSeed(cell + float2(0, 1)), LatticeSeed(cell + float2(1, 1)), blend.x);
    return lerp(bottom, top, blend.y);
}

float4 ArcPixel(ArcSurface s) : COLOR0
{
    float age = saturate(s.Surface.x);
    float depth = saturate(s.Surface.y);
    float phase = AnimationTime;
    float2 drift = float2(age * 7.0 - phase * 1.35, depth * 9.0 + phase * 0.32);
    float slow = SoftField(drift);
    float fine = SoftField(drift * float2(2.7, 1.9) + float2(slow * 1.8, -slow));
    float curl = sin(depth * 17.0 + age * 11.0 - phase * 2.3 + slow * 6.0);
    float cut = 0.14 + age * 0.43 + depth * 0.20;
    float vapor = smoothstep(cut, cut + 0.35, slow * 0.68 + fine * 0.32);
    float rim = 1.0 - smoothstep(0.48, 1.0, depth);
    float tail = pow(1.0 - age, 0.85);
    float body = vapor * rim * tail;
    float filamentPhase = depth * 52.0 + curl * 1.6 + fine * 8.0 - phase * 3.0;
    float filaments = pow(saturate(0.5 + 0.5 * sin(filamentPhase)), 10.0);
    filaments *= vapor * rim * tail * 0.34;
    float railDepth = 0.023 + sin(age * 18.0 + phase) * 0.008;
    float rail = exp2(-abs(depth - railDepth) * 115.0) * tail;
    float glow = exp2(-depth * 17.0) * tail * (0.6 + fine * 0.4);
    float opacity = saturate(body * 0.74 + rail * 0.82 + glow * 0.13);
    float3 pigment = lerp(BodyTint, ShadowTint, smoothstep(0.12, 0.76, depth));
    float3 radiance = pigment * opacity + BodyTint * (glow * 0.28 + filaments);
    radiance += EdgeTint * (rail * 1.18 + filaments * 0.46);
    return float4(radiance, opacity) * s.Tint.a;
}

technique DreadnoughtArc
{
    pass ArcPass
    {
        VertexShader = compile vs_3_0 ArcVertex();
        PixelShader = compile ps_3_0 ArcPixel();
    }
}
