// Original materials for Terra-shaped carriers and their small guided cores.
float4x4 Transform;
float3 BodyTint;
float3 EdgeTint;
float Phase;
float Style;
float Opacity;
struct Vertex { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Vertex v) { Surface s; s.Position=mul(float4(v.Position,1),Transform); s.Color=v.Color; s.UV=v.UV; return s; }
float4 Wave(Surface s):COLOR0 {
    float2 p=s.UV*2-1;
    float r=length(p),a=atan2(p.y,p.x);
    float front=smoothstep(.48,.78,cos(a));
    float ripple=.014*sin(a*(12+Style*2)-Phase*14);
    float outer=exp2(-abs(r-(.79+ripple))*160);
    float inner=exp2(-abs(r-.73)*190)*.5;
    float etching=pow(saturate(cos(a*23+Style*3-Phase*7)),22)*exp2(-abs(r-.765)*65);
    float window=(1-smoothstep(.88,.99,r))*front;
    float alpha=saturate(outer+inner+etching*.48)*window*Opacity;
    return float4(lerp(EdgeTint,float3(1,1,1),outer*.55),alpha);
}
float4 Core(Surface s):COLOR0 {
    float2 p=s.UV*2-1;
    float2 head=p-float2(.15,0);
    float bloom=exp2(-length(head*float2(1.2,1.8))*5);
    float diamond=pow(saturate(1-abs(head.x)*3.2-abs(head.y)*4.5),2);
    float cross=exp2(-abs(head.x)*70-abs(head.y)*8)+exp2(-abs(head.y)*85-abs(head.x)*5);
    float pulse=.86+.14*sin(Phase*21+Style*2);
    float alpha=saturate(bloom*.35+diamond+cross*.66)*pulse*Opacity;
    return float4(lerp(BodyTint,EdgeTint,saturate(bloom*.5+diamond+cross)),alpha);
}
float4 Trail(Surface s):COLOR0 {
    float u=saturate(s.UV.x),v=abs(s.UV.y*2-1);
    float envelope=pow(1-u,1.4)*(1-smoothstep(.64,1,v));
    float spine=exp2(-v*16),filament=pow(saturate(cos(u*25-Phase*19+Style)),8);
    float alpha=envelope*(.25+spine*.75+filament*.12)*Opacity;
    return float4(lerp(BodyTint,EdgeTint,spine*.85),alpha);
}
technique CrusherWave {
    pass WavePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Wave(); }
    pass CorePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Core(); }
    pass TrailPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Trail(); }
}
