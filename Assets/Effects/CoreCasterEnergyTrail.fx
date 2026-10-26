float4x4 Transform;
float3 Outer, Inner, Shadow;
float Time, Style;
struct Input { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Input v) { Surface s; s.Position=mul(float4(v.Position,1),Transform); s.Color=v.Color; s.UV=v.UV; return s; }
float Hash(float2 p) { return frac(sin(dot(p,float2(47.13,91.7)))*14381.37); }
float Noise(float2 p) {
 float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
 return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
}
float4 PS(Surface s):COLOR0 {
 float age=saturate(s.UV.x), side=s.UV.y*2-1;
 float phase=Time*(Style==4?5:2.8);
 float n=Noise(float2(age*9-phase,side*3+Style));
 float fine=Noise(float2(age*19-phase*1.6,side*5)+n);
 float flow=sin(age*19-phase*3+n*3)*age*.15;
 float cross=abs(side+flow);
 float body=(1-smoothstep(.12,1,cross))*lerp(.32,1,n*.65+fine*.35);
 float core=exp2(-abs(side+flow*.5)*18)*(1-age);
 float strand=pow(saturate(.5+.5*sin(side*21+age*18-phase*2+n*5)),7)*body*.26;
 float fade=pow(1-age,1.15)*s.Color.a;
 float smoke=Style==2?1.15:1;
 float coverage=saturate(body*.72*smoke+core*.25)*fade;
 float3 pigment=lerp(Shadow,Outer,saturate(n*.8+fine*.3));
 float3 color=pigment*coverage+Inner*(core*.26+strand*.24)*fade;
 return float4(color,coverage);
}
technique EnergyTrail { pass Energy { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
