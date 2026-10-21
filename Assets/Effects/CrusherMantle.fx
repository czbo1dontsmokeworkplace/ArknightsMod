// Original heavy-blade material. Bounded geometry supplies the real swept path.
// No external textures, third-party effect code, or sampled noise assets.
float4x4 Transform;
float3 BodyTint;
float3 EdgeTint;
float Phase;
float Style;
float Opacity;
struct Vertex { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Vertex v) { Surface s; s.Position=mul(float4(v.Position,1),Transform); s.Color=v.Color; s.UV=v.UV; return s; }
float Hash(float2 p) { return frac(sin(dot(p,float2(71.31,119.73)))*17393.721); }
float Noise(float2 p) {
    float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
}
float Smoke(float2 p) { return Noise(p)*.57+Noise(p*2.07+5.3)*.29+Noise(p*4.13-2.1)*.14; }
float4 Trail(Surface s):COLOR0 {
    float u=saturate(s.UV.x),r=saturate(s.UV.y);
    float n=Smoke(float2(u*8-Phase*1.4,r*5+Phase*.7));
    float stream=Smoke(float2(u*22-Phase*3,r*8+n*3));
    float boundary=.1+Noise(float2(u*18-Phase*2,Style*3))* .18;
    float outer=smoothstep(boundary-.08,boundary+.08,r);
    float inner=1-smoothstep(.78,1,r);
    float tail=pow(1-u,.65)*smoothstep(0,.055,u);
    float folds=pow(saturate(.5+.5*sin(r*32+u*18-Phase*8+n*9)),4);
    float vein=exp2(-abs(r-(.3+.045*sin(u*19+n*4)))*58);
    float rim=exp2(-abs(r-(boundary+.055))*75);
    float flame=smoothstep(.39+u*.25,.76,stream)*outer*inner;
    float alpha=saturate((.46+folds*.24+flame*.35)*outer*inner)*tail*Opacity*s.Color.a;
    float3 body=lerp(BodyTint*.17,BodyTint,.35+folds*.55+n*.2);
    float3 color=lerp(body,EdgeTint,saturate(rim*.92+vein*.58+flame*.36));
    return float4(color,alpha);
}
float4 Seal(Surface s):COLOR0 {
    float2 p=s.UV*2-1;
    float radius=length(p),a=atan2(p.y,p.x);
    float spin=Phase*.23;
    float ring=exp2(-abs(radius-.74)*100)+exp2(-abs(radius-.57)*140)*.42;
    float glyph=pow(saturate(cos((a+spin)*12)),16)*exp2(-abs(radius-.64)*70);
    float segments=smoothstep(-.1,.4,sin((a-spin)*6))*.65+.35;
    float mist=Smoke(p*5+float2(Phase*.3,0));
    float floorGlow=pow(saturate(1-radius),2)*mist*.23;
    float alpha=saturate(ring*segments*.65+glyph*.65+floorGlow)*Opacity;
    return float4(lerp(BodyTint,EdgeTint,saturate(glyph+ring*.3)),alpha);
}
float4 Impact(Surface s):COLOR0 {
    float2 p=s.UV*2-1;
    float radius=length(p),a=atan2(p.y,p.x);
    float n=Smoke(p*9+Style*3);
    float expansion=.15+Phase*.7;
    float ripple=exp2(-abs(radius-expansion-(n-.5)*.045)*85)*pow(saturate(1-Phase),1.6);
    float petals=pow(saturate(cos(a*(5+Style)+n*2)),7)*exp2(-radius*5)*(1-Phase);
    float core=exp2(-length(p*float2(.72,3.8))*10)*pow(1-Phase,3);
    float alpha=saturate(ripple*.72+petals*.55+core)*Opacity*(1-smoothstep(.9,1,radius));
    return float4(lerp(BodyTint,EdgeTint,saturate(core+petals*.55)),alpha);
}
technique CrusherMantle {
    pass TrailPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Trail(); }
    pass SealPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Seal(); }
    pass ImpactPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Impact(); }
}
