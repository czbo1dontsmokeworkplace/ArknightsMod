// A complete ice membrane on a transparent carrier, not CPU-built line geometry.
float Clock, Radius, Width, Extent, GapAngle, GapHalf, Strength;
float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
float Noise(float2 p) {
    float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
}
float4 IceMembrane(float4 tint:COLOR0,float2 uv:TEXCOORD0):COLOR0 {
    float2 p=(uv*2-1)*Extent;
    float r=length(p), angle=atan2(p.y,p.x);
    float gap=GapHalf<=0?1:smoothstep(GapHalf,GapHalf+.025,abs(atan2(sin(angle-GapAngle),cos(angle-GapAngle))));
    float n=Noise(p*.045+float2(Clock*.3,-Clock*.4));
    float distortion=(n-.5)*Width*.34;
    float band=exp2(-pow((r-Radius+distortion)/max(1,Width*.6),2)*3);
    float mist=exp2(-pow((r-Radius+Width*.8)/max(1,Width*1.8),2)*3);
    float crest=exp2(-abs(r-Radius+distortion)*.9);
    float facet=pow(saturate(1-abs(n-.51)*9),3);
    float alpha=saturate((band*(.55+facet*.4)+mist*.18+crest*.18)*Strength*tint.a*gap);
    float3 ice=lerp(float3(.055,.20,.34),float3(.55,.88,1),saturate(band*.65+facet*.3+crest));
    return float4(ice*tint.rgb*alpha,alpha);
}
technique FrostRing { pass Ice { PixelShader=compile ps_3_0 IceMembrane(); } }
