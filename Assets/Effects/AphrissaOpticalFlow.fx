// Original, texture-free optical packet material. Input geometry is actual flight history.
float4x4 WorldView;
float FlowTime;
float Phase;
struct VIn { float2 Position : POSITION0; float4 Tint : COLOR0; float2 UV : TEXCOORD0; };
struct VOut { float4 Position : POSITION0; float4 Tint : COLOR0; float2 UV : TEXCOORD0; };
VOut FlightVertex(VIn i)
{
    VOut o;
    o.Position = mul(float4(i.Position, 0, 1), WorldView);
    o.Tint = i.Tint; o.UV = i.UV;
    return o;
}
float Seed(float2 q) { return frac(sin(dot(q, float2(47.31, 173.7))) * 39181.17); }
float Drift(float2 q)
{
    float2 cell = floor(q), f = frac(q);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(Seed(cell), Seed(cell + float2(1, 0)), f.x),
        lerp(Seed(cell + float2(0, 1)), Seed(cell + 1), f.x), f.y);
}
float4 OpticalPixel(VOut i) : COLOR0
{
    float along = saturate(i.UV.x);
    float across = i.UV.y * 2 - 1;
    float n = Drift(float2(along * 28 - FlowTime * 11 + Phase * 7, across * 4 + FlowTime));
    float curl = sin(along * 32 - FlowTime * 9 + Phase * 6) * .11;
    float d = abs(across + curl * (0.5 + n));
    float envelope = smoothstep(0, .055, along) * pow(1 - along, .6) * smoothstep(0, .06, 1 - along);
    float body = (1 - smoothstep(.12, .95, d)) * (.52 + .48 * n);
    float core = exp2(-d * 12) * (.65 + .35 * n);
    float grain = pow(saturate(n - d * .2), 4) * .7;
    float alpha = saturate((body * .68 + core * .34) * envelope) * i.Tint.a;
    float3 pigment = lerp(i.Tint.rgb * .45, i.Tint.rgb, n);
    float3 color = pigment * alpha + lerp(i.Tint.rgb, float3(1, .97, .81), .42) * (core * .48 + grain) * envelope * i.Tint.a;
    // Premultiplied material output, intentionally paired with AlphaBlend.
    return float4(color, alpha);
}
technique OpticalFlight
{
    pass OpticalPass
    {
        VertexShader = compile vs_3_0 FlightVertex();
        PixelShader = compile ps_3_0 OpticalPixel();
    }
}
