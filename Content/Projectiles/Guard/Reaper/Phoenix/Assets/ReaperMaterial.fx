// Color/alpha/bloom modulation follows the extracted color_bloom shader.
// Filtered downsampling, separable blur and two blur scales are the Terraria adaptation; their
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
float4 Downsample4PS(VO i):COLOR0 {
    float3 c=0;
    [unroll] for(int y=0;y<4;y++) [unroll] for(int x=0;x<4;x++)
        c+=tex2D(SourceSampler,i.uv+(float2(x,y)-1.5)*uTexelStep).rgb;
    return float4(c/16,0);
}
float4 Downsample2PS(VO i):COLOR0 {
    float3 c=tex2D(SourceSampler,i.uv+float2(-.5,-.5)*uTexelStep).rgb;
    c+=tex2D(SourceSampler,i.uv+float2(.5,-.5)*uTexelStep).rgb;
    c+=tex2D(SourceSampler,i.uv+float2(-.5,.5)*uTexelStep).rgb;
    c+=tex2D(SourceSampler,i.uv+float2(.5,.5)*uTexelStep).rgb;
    return float4(c/4,0);
}
// Adjacent bilinear tap pairs cover a continuous 41-texel Gaussian kernel.
// Widening a five-tap kernel's stride leaves visible copies of small particles.
float4 FlarePS(VO i):COLOR0 {
    float3 c=tex2D(SourceSampler,i.uv).rgb*.045355;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*1.495931).rgb+tex2D(SourceSampler,i.uv-uTexelStep*1.495931).rgb)*.089325;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*3.490507).rgb+tex2D(SourceSampler,i.uv-uTexelStep*3.490507).rgb)*.083994;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*5.485085).rgb+tex2D(SourceSampler,i.uv-uTexelStep*5.485085).rgb)*.075187;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*7.479666).rgb+tex2D(SourceSampler,i.uv-uTexelStep*7.479666).rgb)*.064070;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*9.474252).rgb+tex2D(SourceSampler,i.uv-uTexelStep*9.474252).rgb)*.051974;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*11.468845).rgb+tex2D(SourceSampler,i.uv-uTexelStep*11.468845).rgb)*.040137;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*13.463444).rgb+tex2D(SourceSampler,i.uv-uTexelStep*13.463444).rgb)*.029506;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*15.458052).rgb+tex2D(SourceSampler,i.uv-uTexelStep*15.458052).rgb)*.020649;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*17.452670).rgb+tex2D(SourceSampler,i.uv-uTexelStep*17.452670).rgb)*.013757;
    c+=(tex2D(SourceSampler,i.uv+uTexelStep*19.447299).rgb+tex2D(SourceSampler,i.uv-uTexelStep*19.447299).rgb)*.008724;
    return float4(c,0);
}
technique Material {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 MaterialPS();}}
technique Blur {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 BlurPS();}}
technique Copy {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 CopyPS();}}
technique Downsample4 {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Downsample4PS();}}
technique Downsample2 {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Downsample2PS();}}
technique Flare {pass P0 {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 FlarePS();}}
