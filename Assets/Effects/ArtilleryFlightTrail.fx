// Independently authored procedural exhaust. No third-party texture or shader input.
float4x4 TrailTransform;
float3 OuterTint;
float3 CoreTint;
float FlowTime;

struct TrailVertexInput
{
    float3 Position : POSITION0;
    float4 Tint : COLOR0;
    float2 UV : TEXCOORD0;
};
struct TrailSurface
{
    float4 Position : POSITION0;
    float4 Tint : COLOR0;
    float2 UV : TEXCOORD0;
};
TrailSurface TrailVertex(TrailVertexInput v)
{
    TrailSurface s;
    s.Position = mul(float4(v.Position, 1.0), TrailTransform);
    s.Tint = v.Tint;
    s.UV = v.UV;
    return s;
}
float FlameSeed(float2 cell)
{
    return frac(sin(dot(cell, float2(47.13, 91.7))) * 14381.37);
}
float FlameField(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(FlameSeed(cell), FlameSeed(cell + float2(1, 0)), f.x),
        lerp(FlameSeed(cell + float2(0, 1)), FlameSeed(cell + 1), f.x), f.y);
}
float4 TrailPixel(TrailSurface s) : COLOR0
{
    float age = saturate(s.UV.x);
    float side = s.UV.y * 2.0 - 1.0;
    float heat = pow(1.0 - age, 0.85);
    float2 flow = float2(age * 9.0 - FlowTime * 3.8, side * 3.5);
    float soft = FlameField(flow);
    float fine = FlameField(flow * float2(2.4, 1.6) + soft * 1.4);
    float bend = sin(age * 21.0 - FlowTime * 6.0 + soft * 4.0) * age * 0.14;
    float across = abs(side + bend);
    float envelope = 1.0 - smoothstep(0.28, 1.0, across);
    float erosion = smoothstep(age * 0.48, age * 0.48 + 0.40, soft * 0.7 + fine * 0.3);
    float fire = envelope * erosion * heat;
    float core = exp2(-across * 19.0) * heat;
    float strands = pow(saturate(0.5 + 0.5 * sin(side * 27.0 + soft * 9.0 - age * 18.0)), 8.0);
    strands *= fire * 0.22;
    float3 color = OuterTint * (fire * 0.68 + strands) + CoreTint * (core * 0.92 + strands * 0.4);
    // Premultiplied radiance; use One/One additive blending in the renderer.
    return float4(color * s.Tint.a, saturate(fire + core) * s.Tint.a);
}
technique ArtilleryFlight
{
    pass FlamePass
    {
        VertexShader = compile vs_3_0 TrailVertex();
        PixelShader = compile ps_3_0 TrailPixel();
    }
}
