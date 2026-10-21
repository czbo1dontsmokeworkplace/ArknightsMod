// Independently authored flowing scythe material; no third-party textures or shaders.
float4x4 Transform;
float3 Pigment;
float3 Highlight;
float Time;
float Opacity;
struct Input { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Input v) { Surface s; s.Position=mul(float4(v.Position,1),Transform);s.Color=v.Color;s.UV=v.UV;return s; }
float Random(float2 p) { return frac(sin(dot(p,float2(83.59,47.73)))*29813.917); }
float Noise(float2 p) {
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(Random(i),Random(i+float2(1,0)),f.x),lerp(Random(i+float2(0,1)),Random(i+1),f.x),f.y);
}
float4 PS(Surface s):COLOR0 {
    float u=s.UV.x,v=s.UV.y;
    float noise=Noise(float2(u*17-Time*2,v*5+Time*.6));
    float flow=sin(u*37+v*19-Time*8+noise*4)*.5+.5;
    float envelope=pow(saturate(1-u),.55)*smoothstep(0,.06,u);
    float ribbon=smoothstep(0,.11,v)*(1-smoothstep(.72+.12*noise,1,v));
    float outer=exp2(-abs(v-.13-noise*.025)*75);
    float second=exp2(-abs(v-.29-.02*sin(u*28+Time*3))*100)*.65;
    float fibers=pow(saturate(flow),6)*ribbon;
    float alpha=saturate(ribbon*(.14+noise*.12)+outer*1.7+second+fibers*.26)*envelope*Opacity*s.Color.a;
    float3 ink=lerp(Pigment*.4,Pigment,.55+noise*.45);
    float3 color=lerp(ink,lerp(Highlight,float3(1,1,1),.7),saturate(outer*1.7+second+fibers*.5));
    return float4(color,alpha);
}
technique Silk { pass Scythe { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS(); } }
