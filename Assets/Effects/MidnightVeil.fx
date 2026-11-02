// Original ink-blade material: a solid dark silhouette, flowing grain and a restrained cool rim.
float4x4 Transform;
float Time, Opacity;
struct Vertex { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Vertex v) { Surface s; s.Position=mul(float4(v.Position,1),Transform); s.Color=v.Color; s.UV=v.UV; return s; }
float Hash(float2 p) { return frac(sin(dot(p,float2(71.31,119.73)))*17393.721); }
float Noise(float2 p) {
    float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
}
float4 Flight(Surface s):COLOR0 {
    float x=s.UV.x, y=s.UV.y*2-1;
    float n=Noise(float2(x*12-Time*2.5,y*5+Time*.6));
    float grain=Noise(float2(x*34-Time*5,y*13+n*3));
    float bend=sin(x*8-Time*2)*.10*(1-x);
    float width=(.055+.53*sin(x*2.9))*(1-smoothstep(.73,1,x));
    float dist=abs(y+bend), edge=exp2(-abs(dist-width)*95);
    float body=(1-smoothstep(width-.025,width+.035,dist))*smoothstep(.02,.16,x);
    float smoke=(1-smoothstep(width+.03,width+.3,dist))*smoothstep(.15,.85,n)*(1-x)*.25;
    float veins=pow(saturate(grain-.5)*2,4)*body;
    float alpha=saturate(body*.95+smoke+edge*.38)*Opacity;
    float3 ink=lerp(float3(.012,.014,.023),float3(.042,.046,.068),n);
    float3 color=lerp(ink,float3(.34,.35,.46),saturate(edge*.85+veins*.14));
    return float4(color,alpha);
}
float4 Impact(Surface s):COLOR0 {
    float2 p=s.UV*2-1; float r=length(p), a=atan2(p.y,p.x), t=saturate(Time);
    float n=Noise(p*7+float2(t*2,-t*3));
    float radius=.18+sqrt(t)*.57;
    float ring=exp2(-abs(r-radius-(n-.5)*.06)*85);
    float cuts=pow(saturate(cos(a*5+n*2)),12)*exp2(-r*3)*(1-t);
    float ink=(1-smoothstep(radius-.1,radius+.12,r))*(.65+n*.35);
    float coverage=saturate(ink*.65+ring*.6+cuts)*(1-smoothstep(.87,1,r))*Opacity;
    return float4(lerp(float3(.016,.018,.03),float3(.33,.34,.45),saturate(ring*.8+cuts*.2)),coverage);
}
technique NightFlight { pass Ink { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Flight(); } }
technique NightImpact { pass Ink { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Impact(); } }
