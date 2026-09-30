// Original material for the pressure washer: no external sprite or shader samples.
float uTime;
sampler uTexture : register(s0);
float4 WaterPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 lens = tex2D(uTexture, uv);
    float flow = uv.x * 23.0 - uTime * 7.5 + sin(uv.y * 17.0 + uTime * 2.0) * 1.6;
    float caustic = pow(saturate(sin(flow) * 0.5 + 0.5), 12.0);
    float second = pow(saturate(cos(uv.y * 29.0 + uv.x * 11.0 + uTime * 4.0)), 14.0);
    float rim = pow(saturate(1.0 - abs(uv.y - 0.48) * 2.0), 6.0);
    float3 water = lens.rgb + float3(0.32, 0.68, 0.77) * lens.a * (caustic * 0.46 + second * 0.22);
    water += float3(0.14, 0.24, 0.26) * lens.a * rim;
    return float4(water * tint.rgb, lens.a * tint.a);
}
technique ChalterWater
{
    pass WaterPass { PixelShader = compile ps_3_0 WaterPixel(); }
}
