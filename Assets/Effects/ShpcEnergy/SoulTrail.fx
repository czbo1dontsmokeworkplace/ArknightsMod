// SHPC NewSHPS / Calamity ImpFlameTrail, adapted to this mod's two-component ribbon UVs.
texture uStreak;
sampler uImage1 : register(s1) = sampler_state { Texture = <uStreak>; };
matrix uWorldViewProjection;
float uTime;
float uStreakOnly;
struct VertexInput {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};
struct VertexOutput {
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};
VertexOutput VertexMain(VertexInput input) {
    VertexOutput output;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.UV = input.UV;
    return output;
}
float4 PixelMain(VertexOutput input) : COLOR0 {
    float2 uv = input.UV;
    float streak = tex2D(uImage1, float2(frac(uv.x - uTime * 2.9), uv.y)).r;
    float bloom = lerp(pow(saturate(sin(uv.y * 3.14159265)), lerp(3, 10, uv.x)), 0.7, uv.x);
    float brightness = lerp(pow(bloom, 6) * lerp(2, 7, streak), streak, uStreakOnly);
    return input.Color * brightness;
}
technique SoulTrail {
    pass TrailPass {
        VertexShader = compile vs_3_0 VertexMain();
        PixelShader = compile ps_3_0 PixelMain();
    }
}
