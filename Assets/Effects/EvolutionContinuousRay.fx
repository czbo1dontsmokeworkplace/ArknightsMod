// Original continuous energy ray. Uses the mod's grayscale noise, no Calamity shader or boss art.
float uTime;
float uOpacity;
float uLength;
float3 uColor;
float3 uCore;
sampler uNoise : register(s1);
float4 RayPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float x = uv.x;
    float y = uv.y * 2 - 1;
    float n = tex2D(uNoise, float2(x * uLength / 280 - uTime * 2.1, uv.y * 1.8 + uTime * .15)).r;
    float n2 = tex2D(uNoise, float2(x * uLength / 110 + uTime * 1.1, uv.y * 3.6 - uTime * .3)).r;
    float ends = smoothstep(0, .018, x) * smoothstep(0, .035, 1-x);
    float edge = abs(y) + (n - .5) * .11;
    float halo = pow(saturate(1-edge), 3.6);
    float mantle = exp(-y*y*13) * (.65 + n*.5);
    float spine = exp(-y*y*90) * (.9+n2*.12);
    float ripple = sin(x*uLength*.035-uTime*16+y*4) * .045;
    float ribbon = exp(-pow((y-ripple)*8,2)) * (.2+n2*.25);
    float3 rgb = uColor*(halo*.26+mantle*.66) + uCore*(spine*.95+ribbon*.45);
    return float4(rgb * ends * uOpacity * tint.rgb * tint.a, 0);
}
technique ContinuousEnergyRay
{
    pass RayPass { PixelShader = compile ps_3_0 RayPixel(); }
}
