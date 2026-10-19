// Original analytic bloom: no sampled Calamity textures or external effects.
float4x4 WorldView;
float FlowTime;
struct VIn { float4 Position : POSITION0; float4 Tint : COLOR0; float2 UV : TEXCOORD0; };
struct VOut { float4 Position : POSITION0; float4 Tint : COLOR0; float2 UV : TEXCOORD0; };
VOut BloomVertex(VIn i)
{
    VOut o;
    o.Position = mul(float4(i.Position.xy, 0, 1), WorldView);
    o.Tint = i.Tint; o.UV = i.UV;
    return o;
}
float4 BloomPixel(VOut i) : COLOR0
{
    float2 p = i.UV * 2 - 1;
    float r2 = dot(p, p);
    float edge = 1 - smoothstep(.64, 1, r2);
    float bloom = exp2(-r2 * 7.5) * edge;
    // Very faint, moving chromatic skin around the soft volume, never moving its axis.
    float skin = sin(p.x * 11 + FlowTime * 4) * .045 * smoothstep(.10, .55, r2);
    float3 color = lerp(i.Tint.rgb, i.Tint.bgr, max(0, skin));
    // Straight-alpha output paired with real Additive; alpha remains nonzero.
    return float4(color, bloom * i.Tint.a);
}
technique FluxMaterial
{
    pass FluxBloom
    {
        VertexShader = compile vs_3_0 BloomVertex();
        PixelShader = compile ps_3_0 BloomPixel();
    }
}
