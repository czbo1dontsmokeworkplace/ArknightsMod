// Original comet material: turbulent mantle, split luminous threads and a hot cutting head.
float4x4 Transform;
float3 Pigment;
float3 Highlight;
float Time;
float Opacity;
float Seed;
float Mode;
float Progress;
struct Input { float3 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct Surface { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
Surface VS(Input v) { Surface s; s.Position=mul(float4(v.Position,1),Transform);s.Color=v.Color;s.UV=v.UV;return s; }
float Hash(float2 p) { return frac(sin(dot(p,float2(127.13,311.71)))*43719.17); }
float Noise(float2 p) {
    float2 cell=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
}
float4 PS(Surface s):COLOR0 {
    float u=s.UV.x,v=s.UV.y*2-1;
    float n=Noise(float2(u*17-Time*5+Seed,v*3+Time*1.3));
    float hot,body,filaments,halo;
    if(Mode<.5) {
        float taper=pow(saturate(1-u),1.25);
        float side=v+sin(u*23-Time*12+Seed)*.1*u;
        hot=exp2(-side*side*180)*taper;
        body=exp2(-side*side*(7+14*u))*(.4+n*.6)*taper;
        float fork=.28+.1*sin(u*39-Time*9+Seed);
        filaments=exp2(-abs(abs(side)-fork)*55)*pow(saturate(1-u),1.8);
        filaments*=smoothstep(.08,.23,u)*(.5+n*.7);
        halo=exp2(-v*v*3)*taper*.18;
    }
    else if(Mode<1.5) {
        float x=u*2-1;
        float taper=smoothstep(-1,-.45,x)*(1-smoothstep(.3,.95,x));
        float flutter=sin(x*23-Time*16+Seed)*.08+sin(x*41+Time*10)*.03;
        float side=v+flutter*(1-u);
        float width=.16+.34*(1-u)*(.6+n*.6);
        body=exp2(-side*side/(width*width))*taper*(.6+n*.8);
        hot=exp2(-pow((x-.27)*3,2)-v*v*90)*1.5;
        filaments=exp2(-abs(abs(side)-width)*65)*taper*(1-u)*.8;
        halo=exp2(-x*x*2-v*v*4)*.25;
    }
    else {
        float2 p=float2(u*2-1,v);
        float radius=.12+Progress*.7;
        float ring=exp2(-abs(length(p)-radius)*95);
        float rays=(exp2(-abs(p.x)*90)*exp2(-abs(p.y)*4)+exp2(-abs(p.y)*90)*exp2(-abs(p.x)*4));
        hot=exp2(-dot(p,p)*70)*3*(1-Progress)+rays*(1-Progress);
        body=ring*.65;
        filaments=ring*Noise(p*13+Time)*.7;
        halo=exp2(-dot(p,p)*8)*.3;
    }
    float energy=saturate(hot*1.2+body*.6+filaments);
    float alpha=saturate(hot+body*.8+filaments*.65+halo)*Opacity*s.Color.a;
    float3 color=lerp(Pigment*.75,Highlight,energy*.6);
    color=lerp(color,float3(1,1,1),saturate(hot*.9+filaments*.2));
    return float4(color,alpha);
}
technique Wisp { pass Comet { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 PS(); } }
