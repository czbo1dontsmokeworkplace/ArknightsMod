// Color/alpha/bloom modulation follows the extracted color_bloom shader.
// The separable blur and two blur scales are the Terraria adaptation; their
// runtime calibration against the original renderer remains a separate step.
float4x4 uTransform;
float uEmission;
float2 uTexelStep;
float uCompositeGain = 1;
texture uTexture;
sampler SourceSampler = sampler_state {
    Texture = <uTexture>; MinFilter = Linear; MagFilter = Linear; MipFilter = None;
    AddressU = Clamp; AddressV = Clamp;
};
struct VI {float4 p:POSITION0;float4 color:COLOR0;float2 uv:TEXCOORD0;float2 radiance:TEXCOORD1;};
struct VO {float4 p:POSITION0;float4 color:COLOR0;float2 uv:TEXCOORD0;float2 radiance:TEXCOORD1;};
VO VS(VI i) {VO o;o.p=mul(i.p,uTransform);o.color=i.color;o.uv=i.uv;o.radiance=i.radiance;return o;}
float4 MaterialPS(VO i):COLOR0 {
    float4 sample=tex2D(SourceSampler,i.uv);
    float4 c=sample*float4(i.color.rgb*i.color.a,i.color.a);
    if(uEmission>.5) return float4(c.rgb*(uEmission>1.5?i.radiance.y:i.radiance.x),0);
    return c;
}
float4 BlurPS(VO i):COLOR0 {
    float3 c=tex2D(SourceSampler,i.uv).rgb*.2270270270;
    c+=tex2D(SourceSampler,i.uv+uTexelStep*1.3846153846).rgb*.3162162162;
    c+=tex2D(SourceSampler,i.uv-uTexelStep*1.3846153846).rgb*.3162162162;
    c+=tex2D(SourceSampler,i.uv+uTexelStep*3.2307692308).rgb*.0702702703;
    c+=tex2D(SourceSampler,i.uv-uTexelStep*3.2307692308).rgb*.0702702703;
    return float4(c,0);
}
float4 CopyPS(VO i):COLOR0 {return tex2D(SourceSampler,i.uv)*uCompositeGain;}
technique Material {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 MaterialPS();}}
technique Blur {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 BlurPS();}}
technique Copy {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 CopyPS();}}
