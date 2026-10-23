// Original procedural terminal-ballistics material: opaque void, torn violet rim and streaming sparks.
float4x4 WorldView;
float FlowTime;
float Strand;
struct Input { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Input v) { Surface o; o.Position=mul(float4(v.Position,1),WorldView); o.Color=v.Color; o.UV=v.UV; return o; }
float Hash(float2 p) { return frac(sin(dot(p,float2(73.29,137.41)))*31927.17); }
float Noise(float2 p) {
    float2 a=floor(p), f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
}
float4 PS(Surface s):COLOR0 {
    float u=saturate(s.UV.x), v=s.UV.y*2-1;
    float tail=pow(1-u,1.3);
    float n=Noise(float2(u*19-FlowTime*9,v*3+FlowTime));
    float d=abs(v + sin(u*26-FlowTime*17)*.045*u);
    float boundary=.40+n*.22;
    float fill=1-smoothstep(boundary-.10,boundary+.04,d);
    float rim=exp2(-abs(d-boundary)*38);
    float halo=exp2(-d*4)*.25;
    float streak=pow(saturate(sin(u*65-FlowTime*30+v*11)),14)*n;
    float3 violet=float3(.49,.13,1.0), pale=float3(.91,.8,1.0);
    float side=step(.5,abs(Strand));
    float alpha=saturate(fill*(1-side)*.96+rim*.65+halo*.35)*tail;
    float3 color=float3(.012,.004,.025)*fill*tail;
    color+=(violet*(rim*.95+halo*.35)+pale*rim*streak*.85)*tail;
    color=lerp(color,(violet*.55+pale*streak)*exp2(-d*6)*tail*.7,side);
    alpha=lerp(alpha,exp2(-d*6)*tail*.5,side);
    return float4(color*s.Color.a,alpha*s.Color.a);
}
technique TerminalWake { pass Wake { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
