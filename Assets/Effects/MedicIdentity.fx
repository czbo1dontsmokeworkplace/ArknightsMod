// Original procedural medical silhouettes. No sampled third-party artwork.
float4x4 Transform;
float3 BodyTint, CoreTint;
float Clock, Opacity;
struct V { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct O { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
O VS(V v) { O o; o.Position=mul(float4(v.Position,1),Transform);o.Color=v.Color;o.UV=v.UV;return o; }
float stroke(float2 p,float2 a,float2 b,float w) {float2 v=b-a;return 1-smoothstep(w,w+.035,length(p-a-v*saturate(dot(p-a,v)/max(dot(v,v),.001))));}
float ring(float2 p,float r,float w) {return 1-smoothstep(w,w+.035,abs(length(p)-r));}
float oval(float2 p,float2 size) {return 1-smoothstep(.8,1,length(p/size));}
float diamond(float2 p,float2 size) {return 1-smoothstep(.8,1,abs(p.x/size.x)+abs(p.y/size.y));}
float polygon(float2 p,float n,float r,float w) {float a=atan2(p.y,p.x);float d=cos(floor(.5+a/(6.283185/n))*(6.283185/n)-a)*length(p);return 1-smoothstep(w,w+.025,abs(d-r));}
float4 PS(O v, uniform float Style):COLOR0
{
    float2 p=v.UV*2-1; float a=atan2(p.y,p.x), r=length(p), t=Clock;
    float s=0,hot=0;
    if(Style<4.5) {s=polygon(p,6,.52,.045)+polygon(p,6,.32+.025*sin(t*3),.025);hot=stroke(p,float2(-.19,0),float2(.19,0),.04)+stroke(p,float2(0,-.19),float2(0,.19),.04);}
    else if(Style<10.5) {s=ring(p,.27,.055)+stroke(p,float2(-.12,-.4),float2(.12,-.4),.08);s+=oval(p,float2(.65,.35))*.18;hot=pow(saturate(cos(a*5+t*.5)),8)*ring(p,.62,.08);}
    else if(Style<11.5) {float blade=r-(.42+.16*sin(a*3-t*3));s=1-smoothstep(.025,.07,abs(blade));hot=oval(p,float2(.13,.13))+ring(p,.78,.02)*(.5+.5*sin(a*3-t*3));}
    else {s=oval(float2(r-.4,0),float2(.2,1))*pow(.5+.5*cos(a*5+t),3);hot=oval(p,float2(.12,.12));}
    float alpha=saturate(max(s,hot))*Opacity*v.Color.a;
    return float4(lerp(BodyTint,CoreTint,saturate(hot)) * alpha,alpha);
}
technique Identity4 {pass Body {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS(4);}}
technique Identity10 {pass Body {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS(10);}}
technique Identity11 {pass Body {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS(11);}}
technique PerfumePetals {pass Body {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS(22);}}
