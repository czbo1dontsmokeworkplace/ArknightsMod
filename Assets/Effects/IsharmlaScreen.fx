sampler uImage0 : register(s0);
float uTime;float uOpacity;float2 WaveCenter;float Aspect;float Pulse;
float4 OceanPressure(float4 position : SV_POSITION,float2 uv:TEXCOORD0):COLOR0
{
    float2 delta=(uv-WaveCenter)*float2(Aspect,1);
    float r=max(.001,length(delta));float phase=frac(uTime*.42);
    float band=exp(-pow((r-phase*1.8)*18,2))*sin(phase*3.14159);
    float2 offset=delta/r*float2(1/Aspect,1)*band*.008*Pulse*uOpacity;
    float4 col=tex2D(uImage0,clamp(uv+offset,.001,.999));
    float3 blue=col.rgb*float3(.78,.96,1.12)+float3(.004,.014,.028);
    col.rgb=lerp(col.rgb,blue,saturate(uOpacity));
    return col;
}
technique Ocean {pass OceanPressure {PixelShader=compile ps_3_0 OceanPressure();}}
