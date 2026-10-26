float4x4 Transform;
float3 Outer, Inner, Shadow;
float Time, Style;
texture FlowTexture;
sampler FlowSampler=sampler_state { Texture=<FlowTexture>; MinFilter=Linear; MagFilter=Linear; MipFilter=Linear; AddressU=Wrap; AddressV=Clamp; };
struct Input { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Input v) { Surface s; s.Position=mul(float4(v.Position,1),Transform); s.Color=v.Color; s.UV=v.UV; return s; }
float4 PS(Surface s):COLOR0 {
 float age=saturate(s.UV.x), side=abs(s.UV.y*2-1);
 float fade=pow(1-age,1.3)*s.Color.a;
 float flow=tex2D(FlowSampler,float2(age*1.8-Time*2.6+Style*.17,s.UV.y)).r;
 float shape=pow(saturate(1-side),1.25);
 float filament=pow(saturate(1-side*4),2.5)*(1-age);
 float grain=tex2D(FlowSampler,float2(age*2.2-Time*3.8, .35+s.UV.y*.3)).r;
 float coverage=shape*fade*(.6+flow*.4);
 float3 pigment=lerp(Shadow,Outer,.65+flow*.35);
 // Colored body, source streak flowing within the real path, small pale spine near the head.
 float3 rgb=pigment*coverage + Outer*flow*shape*fade*.62 + Inner*filament*fade*.8;
 return float4(rgb,coverage*.62);
}
technique EnergyTrail { pass Energy { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
