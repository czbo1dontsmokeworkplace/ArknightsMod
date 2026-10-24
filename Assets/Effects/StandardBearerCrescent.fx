// Original filled spear-pressure sheet. All features are clipped to a 32x32 local quad.
float4x4 Transform;
float3 Tint;
float Age;
struct Vertex { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float2 UV:TEXCOORD0; };
Surface VS(Vertex v) { Surface s; s.Position=mul(float4(v.Position,1),Transform); s.UV=v.UV; return s; }
float Hash(float2 p) { return frac(sin(dot(p,float2(83.17,237.29)))*15317.13); }
float Noise(float2 p) {
    float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
}
float4 PS(Surface s):COLOR0 {
    float2 p=s.UV*2-1;
    float side=abs(p.y);
    float front=.77-.98*p.y*p.y;
    float width=.68*pow(saturate(1-p.y*p.y),.7);
    float rear=front-width;
    float head=1-smoothstep(front-.055,front+.055,p.x);
    float tail=smoothstep(rear-.1,rear+.12,p.x);
    float ends=1-smoothstep(.86,1,side);
    float n=Noise(float2(p.x*12-Age*9,p.y*15));
    float along=front-p.x;
    float flow=.5+.5*sin(along*19+p.y*3-Age*13+n*1.8);
    float decay=smoothstep(.48,.95,Age);
    float erosion=lerp(1,smoothstep(decay-.2,decay+.14,n*.65+(p.x-rear)*.4),decay);
    float enter=smoothstep(0,.075,Age+.04);
    float leave=pow(saturate(1-smoothstep(.32,1,Age)),1.25);
    float volume=head*tail*ends*erosion;
    float edge=exp2(-abs(p.x-(front-.065))*35)*ends;
    float veins=pow(flow,5)*volume;
    float alpha=saturate(volume*(.63+flow*.19)+edge*.65)*enter*leave;
    float3 color=lerp(Tint*.42,Tint,.4+n*.55+veins*.18);
    color=lerp(color,lerp(Tint,float3(1,1,1),.68),saturate(edge*.92));
    return float4(color,alpha);
}
technique StandardBearerCrescent { pass Sheet { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
