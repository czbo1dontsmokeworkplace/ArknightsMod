// Original lance slipstream: finite history mesh, flowing colored folds and eroded edges.
float4x4 WorldView;
float FlowTime;
float3 BodyTint;
float3 EdgeTint;
struct Input { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Input v) { Surface o; o.Position=mul(float4(v.Position,1),WorldView); o.Color=v.Color; o.UV=v.UV; return o; }
float Seed(float2 p) { return frac(sin(dot(p,float2(43.17,113.81)))*17923.713); }
float Noise(float2 p) {
    float2 a=floor(p), b=frac(p); b=b*b*(3-2*b);
    return lerp(lerp(Seed(a),Seed(a+float2(1,0)),b.x),lerp(Seed(a+float2(0,1)),Seed(a+1),b.x),b.y);
}
float4 PS(Surface s):COLOR0 {
    float u=saturate(s.UV.x), v=s.UV.y*2-1;
    float n=Noise(float2(u*11-FlowTime*3,v*3+FlowTime*.6));
    float bend=sin(u*18-FlowTime*5+n*4)*(.08+.16*u);
    float d=abs(v-bend);
    float boundary=1-smoothstep(.38+n*.16,.92,d);
    float tail=pow(1-u,1.35)*smoothstep(0,.06,u);
    float folds=pow(saturate(.5+.5*sin(v*21+u*28-FlowTime*8+n*5)),5);
    float mist=smoothstep(.26+u*.35,.78,n)*boundary;
    float rail=exp2(-abs(d-.19)*34)*(1-u);
    float core=exp2(-d*11)*(.45+.55*n);
    float opacity=saturate((mist*.42+core*.26+rail*.35)*tail)*s.Color.a;
    float3 pigment=lerp(BodyTint*.26,BodyTint,.4+folds*.6);
    // Premultiplied body with a restrained emissive rail, used with AlphaBlend.
    float3 rgb=pigment*opacity+EdgeTint*(rail*.33+folds*mist*.12)*tail*s.Color.a;
    return float4(rgb,opacity);
}
technique ChargeWake { pass Wake { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
