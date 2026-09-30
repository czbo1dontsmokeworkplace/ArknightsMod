// Original Evolution material: counter-flowing crimson filaments inside a stable hot core.
// Grayscale noise is owned by this mod; no Infernum shader or texture is embedded here.
float uTime;
float uOpacity;
sampler uNoise : register(s1);

float4 HemalPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float x = uv.x;
    float cross = uv.y * 2.0 - 1.0;
    float n1 = tex2D(uNoise, float2(x * 12.0 - uTime * 0.48, uv.y * 2.2 + uTime * 0.07)).r;
    float n2 = tex2D(uNoise, float2(x * 23.0 + uTime * 0.31, uv.y * 3.7 - uTime * 0.12)).r;
    float envelope = smoothstep(0.0, 0.055, x) * smoothstep(0.0, 0.055, 1.0 - x);
    float edge = abs(cross) + (n1 - 0.5) * 0.18;
    float mantle = pow(saturate(1.0 - edge), 1.5);
    mantle *= 1.0 - smoothstep(0.76, 1.0, abs(cross));
    float core = pow(saturate(1.0 - abs(cross) * 4.7), 2.0);
    float braidA = cross - sin(x * 58.0 - uTime * 4.2) * 0.28;
    float braidB = cross + sin(x * 43.0 + uTime * 3.1) * 0.34;
    float filaments = pow(saturate(1.0 - abs(braidA) * 13.0), 3.0)
                    + pow(saturate(1.0 - abs(braidB) * 16.0), 3.0);
    float surge = 0.78 + n1 * 0.3 + n2 * 0.24;
    float3 ruby = float3(1.0, 0.008, 0.06);
    float3 scarlet = float3(1.0, 0.08, 0.14);
    float3 hot = float3(1.0, 0.88, 0.84);
    float3 color = lerp(ruby, scarlet, n2) * mantle * surge;
    color += scarlet * filaments * mantle * 0.72;
    color += hot * core * 1.1;
    float visibility = envelope * uOpacity * tint.a;
    // Premultiplied additive color: soft falloff is in RGB, never a solid rectangular carrier.
    return float4(color * visibility * tint.rgb, 0);
}
technique EvolutionHemalAxis
{
    pass HemalPass { PixelShader = compile ps_3_0 HemalPixel(); }
}
