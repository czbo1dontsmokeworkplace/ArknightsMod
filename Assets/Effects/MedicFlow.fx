// Original filled, pointed ribbon. A textured surface, never an outline of the path.
float4x4 Transform;
texture FlowTexture;
sampler FlowSampler=sampler_state {Texture=<FlowTexture>;MinFilter=Linear;MagFilter=Linear;MipFilter=Linear;AddressU=Wrap;AddressV=Clamp;};
float Clock,Pattern;
struct V {float3 Position:POSITION0;float4 Color:COLOR0;float2 UV:TEXCOORD0;};
struct O {float4 Position:POSITION0;float4 Color:COLOR0;float2 UV:TEXCOORD0;};
O VS(V v){O o;o.Position=mul(float4(v.Position,1),Transform);o.Color=v.Color;o.UV=v.UV;return o;}
float4 PS(O v):COLOR0 {
    float cross=abs(v.UV.y*2-1);
    float edge=pow(saturate(1-cross),.7);
    float fiber=tex2D(FlowSampler,float2(v.UV.x*1.7-Clock*.6,v.UV.y)).r;
    float core=pow(saturate(1-cross*2.8),2);
    float density=lerp(.8,.45+fiber*.8,saturate(Pattern));
    float a=edge*density*v.Color.a;
    return float4(v.Color.rgb*(.85+core*.5)*a,a);
}
technique Flow {pass Body {VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS();}}
