// Original rising-strike material. Local +X is flight direction.
float4x4 Transform;
float3 BodyTint;
float3 EdgeTint;
float Phase;
float Style;
float Opacity;
struct Vertex { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Vertex v) { Surface s; s.Position=mul(float4(v.Position,1),Transform); s.Color=v.Color; s.UV=v.UV; return s; }
float4 Eruption(Surface s):COLOR0 {
    float2 p=s.UV*2-1;
    float x=p.x, y=abs(p.y);
    float envelope=saturate(1-abs(x))*(1-smoothstep(.72,1,y));
    float spine=exp2(-y*23)*envelope;
    float shell=0, core=0;
    if(Style<.5) {
        float diamond=abs(x*.86)+y*1.8;
        shell=exp2(-abs(diamond-.72)*85)*envelope;
        core=pow(saturate(1-diamond/.72),.6);
        float facet=exp2(-abs(p.y-x*.32)*95)*saturate(1-diamond);
        core+=facet*.8;
    } else if(Style<1.5) {
        float arc=abs(p.y-sin(x*3.4-Phase*18)*.21);
        shell=exp2(-arc*35)*envelope;
        core=exp2(-y*45)*envelope;
        shell+=exp2(-abs(y-(.46-x*.18))*65)*envelope*.65;
    } else if(Style<2.5) {
        float tongue=abs(p.y+sin(x*15-Phase*32)*.12);
        shell=exp2(-tongue*9)*envelope;
        core=exp2(-tongue*35)*envelope;
        shell*=.65+.35*sin(x*38-Phase*46);
    } else {
        shell=exp2(-abs(y-(.32-x*.19))*55)*envelope;
        core=exp2(-y*15)*envelope;
        shell+=exp2(-abs(p.y-sin(x*18-Phase*28)*.15)*38)*envelope*.6;
    }
    float light=saturate(core+spine*.65);
    float alpha=saturate(shell*.9+core+spine)*Opacity;
    return float4(lerp(BodyTint,lerp(EdgeTint,float3(1,1,1),light*.8),saturate(light+shell*.4)),alpha);
}
technique CrusherUpheaval {
    pass BodyPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Eruption(); }
}
