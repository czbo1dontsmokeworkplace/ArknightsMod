float uTime;
float uBurnDuration;

float4 FireBurningPS(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float progress = saturate(uTime / (uBurnDuration + 0.001));
    float flow = uTime * 2.2;
    float wave = sin(uv.y * 13.0 - flow) * 0.075;
    wave += sin(uv.y * 27.0 + flow * 1.37) * 0.035;
    float center = 0.5 + wave;
    float width = lerp(0.48, 0.075, uv.y);
    float body = saturate(1.0 - abs(uv.x - center) / width);
    float height = saturate((1.0 - uv.y) * 4.0);
    float flame = body * height;
    float cutoff = saturate((progress - 0.70) * 3.333);
    flame *= 1.0 - cutoff * saturate((1.0 - uv.y) * 2.0);
    flame *= 1.0 - saturate((progress - 0.90) * 10.0);
    float3 flameColor = lerp(float3(0.95, 0.035, 0.005), float3(1.0, 0.42, 0.035), saturate(flame * 1.8));
    return float4(flameColor * color.rgb, flame * color.a);
}

technique FireBurning
{
    pass FireBurningPass
    {
        PixelShader = compile ps_3_0 FireBurningPS();
    }
}
