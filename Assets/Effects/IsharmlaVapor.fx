float4x4 uTransform;
float uTime;
struct Varying { float4 Position : POSITION0; float4 Color : COLOR0; float2 UV : TEXCOORD0; };
Varying Transform(float4 position : POSITION0, float4 color : COLOR0, float2 uv : TEXCOORD0)
{
    Varying o; o.Position = mul(position, uTransform); o.Color = color; o.UV = uv; return o;
}
float HashV(float2 p) { return frac(sin(dot(p,float2(73.67,219.31))) * 42758.53); }
float NoiseV(float2 p)
{
    float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(HashV(i),HashV(i+float2(1,0)),f.x),lerp(HashV(i+float2(0,1)),HashV(i+1),f.x),f.y);
}
float CloudV(float2 p)
{
    return NoiseV(p)*.56 + NoiseV(p*2.03+7.1)*.29 + NoiseV(p*4.11-3.7)*.15;
}
float4 VaporPixel(Varying i) : COLOR0
{
    float2 p=i.UV*2-1;
    float n=CloudV(p*float2(2.6,2.3)+float2(uTime*.21,-uTime*.14));
    float fold=CloudV(p*3.4+float2(n*1.7-uTime*.16,uTime*.12));
    float r=length(p*float2(.98,1.08));
    float bounds=(1-smoothstep(.76,1,abs(p.x)))*(1-smoothstep(.72,1,abs(p.y)));
    float density=smoothstep(.18,.76,1-r+(n-.47)*.70);
    float curls=pow(saturate(1-abs(fold-.51)*5),2);
    float holes=smoothstep(.15,.40,n);
    float a=density*(.32+fold*.49+curls*.18)*holes*bounds*i.Color.a;
    float3 base=lerp(float3(.09,.22,.31),float3(.58,.78,.84),fold*.75+curls*.25);
    float3 tint=saturate(i.Color.rgb/max(i.Color.a,.001));
    float3 rgb=lerp(base,tint,saturate(fold*.48+curls*.24));
    return float4(rgb*a,a);
}
float4 CurrentPixel(Varying i) : COLOR0
{
    float2 uv=i.UV;
    float n=CloudV(float2(uv.x*5.8-uTime*.48,uv.y*2.6+uTime*.1));
    float bend=sin(uv.x*12-uTime*1.6)*.085+(n-.5)*.12;
    float y=abs(uv.y-.5+bend);
    float body=1-smoothstep(.19,.49,y);
    float strand=pow(saturate(1-abs(y-.19-n*.07)*11),2);
    float spine=pow(saturate(1-abs(uv.y-.5+bend*.4)*7),3);
    float strands=pow(saturate(sin(uv.y*12+n*3+uv.x*5-uTime*1.5)),5);
    float tip=smoothstep(0,.08,uv.x)*(1-smoothstep(.82,1,uv.x));
    float a=body*(.20+n*.24)+strand*.48+spine*.12+strands*body*.1;
    a=saturate(a)*tip*i.Color.a;
    float3 rgb=lerp(float3(.07,.32,.43),saturate(i.Color.rgb/max(i.Color.a,.001)),saturate(strand*.65+spine*.4+n*.35));
    rgb=lerp(rgb,float3(.87,.97,1),strands*strand*.5);
    return float4(rgb*a,a);
}
float4 FilmPixel(Varying i) : COLOR0
{
    float2 p=i.UV*2-1; float r=length(p); float angle=atan2(p.y,p.x);
    float n=CloudV(p*3.3+float2(uTime*.13,-uTime*.17));
    float rim=pow(saturate(1-abs(r-.80-(n-.5)*.065)*20),2);
    float rivulet=pow(saturate(1-abs(n-.51)*15),3)*(1-smoothstep(.75,.94,r));
    float illumination=.24+.76*pow(saturate(cos(angle+1.1)),2);
    float a=(rim*illumination*.68+rivulet*.095)*(1-smoothstep(.91,1,r))*i.Color.a;
    float3 rgb=lerp(float3(.16,.44,.54),saturate(i.Color.rgb/max(i.Color.a,.001)),illumination*.8);
    return float4(rgb*a,a);
}
technique Vapor { pass P { VertexShader=compile vs_3_0 Transform(); PixelShader=compile ps_3_0 VaporPixel(); } }
technique Current { pass P { VertexShader=compile vs_3_0 Transform(); PixelShader=compile ps_3_0 CurrentPixel(); } }
technique Film { pass P { VertexShader=compile vs_3_0 Transform(); PixelShader=compile ps_3_0 FilmPixel(); } }
