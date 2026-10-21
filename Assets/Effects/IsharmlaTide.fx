// Original tide material. No third-party shader, texture or compiled effect is used.
float uTime;
float uStrength;
float2 uTexel;
float2 uSize;
texture uImage;
texture uDetail;
sampler Image : register(s0) = sampler_state { Texture = <uImage>; };
sampler Detail : register(s1) = sampler_state { Texture = <uDetail>; AddressU = Wrap; AddressV = Wrap; };
float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float Noise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(Hash(i), Hash(i + float2(1,0)), f.x), lerp(Hash(i + float2(0,1)), Hash(i + 1), f.x), f.y);
}
float4 WaterPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = float2(uv.x * 17 - uTime * 1.7, uv.y * 4);
    float flow = Noise(p) * .65 + Noise(p * 2.1 + float2(-uTime, uTime * .3)) * .35;
    float bend = sin(uv.x * 26 - uTime * 6) * .06 + (flow - .5) * .12;
    float y = abs(uv.y - .5 + bend);
    float body = 1 - smoothstep(.27, .50, y);
    float caustic = pow(saturate(1 - abs(flow - .5) * 9), 3);
    float threads = pow(saturate(sin(uv.y * 43 + flow * 8 + uv.x * 22 - uTime * 8)), 12);
    float taper = smoothstep(0, .025, uv.x) * (1 - smoothstep(.965, 1, uv.x));
    float a = saturate(body * (.44 + caustic * .43 + threads * .22)) * taper * tint.a;
    float3 rgb = lerp(float3(.002,.035,.32), float3(0,.48,1), caustic * .6 + flow * .45);
    rgb = lerp(rgb, float3(.20,.90,1), threads * .65);
    return float4(rgb * a, a);
}
float4 SpherePixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * 2 - 1; float r = length(p);
    float rim = pow(saturate(1 - abs(r - .88) * 11), 2);
    float n = Noise(p * 4 + float2(uTime * .2, -uTime * .3));
    float caustic = pow(saturate(1 - abs(n - .5) * 10), 3);
    float a = (rim * .85 + caustic * .12 + .02) * (1 - smoothstep(.94, 1, r)) * tint.a;
    return float4(tint.rgb * a, a);
}
float4 HullPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(Image, uv);
    float n = Noise(uv * float2(15, 11) + float2(uTime * .35, -uTime * .55));
    float flow = pow(saturate(1 - abs(n - .52) * 15), 2);
    float a = source.a * tint.a * uStrength * (.025 + flow * .48);
    return float4((tint.rgb + flow * .3) * a, a);
}
float4 OutlinePixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float center = tex2D(Image, uv).a;
    float2 d = uTexel * 2.5;
    float n = max(max(tex2D(Image, uv + float2(d.x,0)).a, tex2D(Image, uv - float2(d.x,0)).a),
        max(tex2D(Image, uv + float2(0,d.y)).a, tex2D(Image, uv - float2(0,d.y)).a));
    float inner = min(min(tex2D(Image, uv + float2(d.x,0)).a, tex2D(Image, uv - float2(d.x,0)).a),
        min(tex2D(Image, uv + float2(0,d.y)).a, tex2D(Image, uv - float2(0,d.y)).a));
    float rim = saturate(n - center) + saturate(center - inner) * .25;
    float a = rim * tint.a;
    return float4(tint.rgb * a, a);
}
float4 PortalPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * 2 - 1; float r = length(p); float angle = atan2(p.y, p.x);
    float bend = sin(angle * 6 - uTime * 3 + r * 8) * .025;
    float ring = pow(saturate(1 - abs(r - .74 + bend) * 15), 2);
    float inner = pow(saturate(1 - abs(r - .62) * 25), 2) * (.5 + .5 * sin(angle * 18 + uTime * 6));
    float spiral = pow(saturate(sin(angle * 4 + r * 28 - uTime * 8)), 16) * smoothstep(.3, .65, r) * (1 - smoothstep(.8, .94, r));
    float core = (1 - smoothstep(.5, .70, r)) * .42;
    float a = saturate(ring * .9 + inner * .4 + spiral * .25 + core) * tint.a * saturate(uStrength * 3);
    float3 rgb = tint.rgb * (ring + inner * .6 + spiral * .5) + float3(.008,.018,.04) * core;
    return float4(rgb * tint.a, a);
}
float4 HaloPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * 2 - 1; float r = length(p); float angle = atan2(p.y, p.x);
    float arc = pow(saturate(1 - abs(r - .68 - sin(angle * 3 + uTime) * .015) * 28), 2);
    arc *= .25 + .75 * pow(saturate(sin(angle * 3 - uTime * 1.5)), 2);
    float dust = pow(saturate(sin(angle * 39 + r * 27 + uTime * 2)), 25) * pow(saturate(1 - abs(r - .77) * 8), 2);
    float a = (arc + dust * .35) * tint.a * uStrength;
    return float4(tint.rgb * a, a);
}
float4 SigilPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * 2 - 1; float r = length(p); float angle = atan2(p.y, p.x);
    float rim = pow(saturate(1 - abs(r - .77) * 40), 2) + pow(saturate(1 - abs(r - .59) * 45), 2) * .45;
    float petals = pow(saturate(1 - abs(r - .44 - sin(angle * 5 + uTime * .7) * .12) * 35), 2) * .5;
    float marks = pow(saturate(cos(angle * 24 - uTime * .4)), 32) * step(.67,r) * step(r,.84);
    float a = saturate(rim + petals + marks * .65) * tint.a;
    return float4(tint.rgb * a, a);
}
float4 CloudPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * 2 - 1;
    float n = Noise(float2(p.x * 4 + uTime * .1, p.y * 3)) * .65 + Noise(p * 8 - uTime * .15) * .35;
    float edge = saturate(1 - length(float2(p.x * .88, p.y * 1.12)) + (n - .5) * .6);
    float bounds = (1 - smoothstep(.72,1,abs(p.x))) * (1 - smoothstep(.68,1,abs(p.y)));
    float alpha = smoothstep(.05,.4,edge) * (.25 + n * .45) * tint.a * bounds;
    float3 rgb = lerp(float3(.025,.10,.16), tint.rgb, n * .65 + pow(saturate(1 - edge * 5),2) * .25);
    return float4(rgb * alpha, alpha);
}
float4 LensPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * 2 - 1; float r = length(p);
    float rim = pow(saturate(1 - abs(r - .72) * 15), 2);
    float body = pow(saturate(1 - r), 1.2);
    float highlight = pow(saturate(1 - length((p - float2(-.22,-.27)) * float2(5,8))), 2);
    float3 dispersion = lerp(float3(.04,.25,.60), float3(.26,.95,1), saturate(p.x + .6));
    float a = saturate(body * .68 + rim * .9 + highlight) * tint.a;
    return float4((dispersion * (body * .65 + rim) + highlight) * tint.a, a);
}
float4 SurgePixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv;
    float crest = .24 + sin(p.x * 4 + uTime * 2) * .08;
    float body = smoothstep(crest - .04, crest + .12, p.y) * (1 - smoothstep(.78, 1, p.x));
    float foam = pow(saturate(1 - abs(p.y - crest) * 22), 2);
    float n = Noise(p * float2(6,10) + float2(-uTime * 2,uTime));
    float a = saturate(body * (.14 + n * .22) + foam * .85) * tint.a;
    return float4(lerp(tint.rgb * .6, float3(.85,1,1), foam) * a, a);
}
float4 ParticleBodyPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(Image, uv);
    float gray = dot(source.rgb, float3(.299,.587,.114));
    float density = source.a * smoothstep(.035,.20,gray);
    float alpha = saturate(density * .96) * tint.a;
    float3 color = lerp(float3(.002,.009,.055), float3(.0,.055,.30), pow(saturate(gray), .65));
    return float4(color * alpha, alpha);
}
float4 ParticleCrestPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(Image, uv);
    float gray = dot(source.rgb, float3(.299,.587,.114));
    float crest = pow(saturate(gray * 1.45 - .16), 1.3);
    float alpha = source.a * crest * tint.a;
    float3 color = lerp(float3(.005,.10,.70), saturate(tint.rgb * 1.8 + float3(0,.18,.08)), crest);
    color = lerp(color, float3(.25,.95,1), pow(saturate(gray * 1.1), 7) * .48);
    return float4(color * alpha, alpha);
}
float4 ParticleGlowPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(Image, uv); float gray = dot(source.rgb,float3(.299,.587,.114));
    float alpha = source.a * pow(saturate(gray * 1.5), 1.4) * tint.a;
    float3 color = lerp(saturate(tint.rgb * 1.9), float3(.72,1,1), pow(saturate(gray),5) * .5);
    return float4(color * alpha * 1.7, alpha);
}
float4 DisintegrationPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float x = uv.x * uSize.x;
    float y = abs(uv.y * 2 - 1);
    float2 sampleUV = float2((2 + frac(x * .0019 - uTime * 4.8)) / 3, frac(uv.y * 1.8 + uTime * .9) / 3);
    float textureNoise = dot(tex2D(Detail,sampleUV).rgb, float3(.299,.587,.114));
    float grain = Noise(float2(x * .045 - uTime * 39, uv.y * 15 + uTime * 8));
    float boundary = .60 + (textureNoise - .4) * .10 + sin(x * .11 - uTime * 105) * .025;
    float body = 1 - smoothstep(boundary - .04, boundary + .035, y);
    float outer = pow(saturate(1 - abs(y - boundary + .025) * 20), 2);
    float pulse = pow(saturate(sin(x * .035 - uTime * 95)), 6);
    float core = 1 - smoothstep(.20 + grain * .025, .37 + grain * .02, y);
    float fractures = pow(saturate(1 - abs(textureNoise - .53) * 10),3) * body;
    float tip = smoothstep(0,.012,uv.x) * (1-smoothstep(.975,1,uv.x));
    float alpha = saturate(body * .98 + outer * .8) * tint.a * tip;
    float3 color = lerp(float3(.025,.16,.25), float3(.07,.43,.62), body);
    color = lerp(color,float3(.34,.84,.92),fractures * .65 + outer * .45);
    color = lerp(color,float3(.58,.98,1),pulse * body * .50);
    color = lerp(color,float3(.93,1,1),core * (.90 + pulse * .10));
    float filament = pow(saturate(sin(x * .14 - uTime * 165 + uv.y * 29 + textureNoise * 9)),12);
    color = lerp(color,float3(.29,.68,.78),filament * core * .19);
    return float4(color * alpha,alpha);
}
float4 PressureMuzzlePixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * 2 - 1; float x = (p.x + 1) * .5;
    float width = .18 + sin(x * 3.14159) * .46;
    float rib = pow(saturate(sin(x * 35 - uTime * 85 + p.y * 5)),4);
    float shape = (1 - smoothstep(width - .06,width + .10,abs(p.y))) * smoothstep(0,.08,x) * (1-smoothstep(.70,1,x));
    float core = pow(saturate(1-abs(p.y) * 4),2);
    float alpha = shape * tint.a;
    float3 color = lerp(float3(.035,.20,.34),float3(.24,.73,.84),rib * .75);
    color = lerp(color,float3(.91,1,1),core * .95);
    return float4(color * alpha,alpha);
}
technique Water { pass P0 { PixelShader = compile ps_3_0 WaterPixel(); } }
technique Sphere { pass P0 { PixelShader = compile ps_3_0 SpherePixel(); } }
technique Hull { pass P0 { PixelShader = compile ps_3_0 HullPixel(); } }
technique Outline { pass P0 { PixelShader = compile ps_3_0 OutlinePixel(); } }
technique Portal { pass P0 { PixelShader = compile ps_3_0 PortalPixel(); } }
technique Halo { pass P0 { PixelShader = compile ps_3_0 HaloPixel(); } }
technique Sigil { pass P0 { PixelShader = compile ps_3_0 SigilPixel(); } }
technique Cloud { pass P0 { PixelShader = compile ps_3_0 CloudPixel(); } }
technique Lens { pass P0 { PixelShader = compile ps_3_0 LensPixel(); } }
technique Surge { pass P0 { PixelShader = compile ps_3_0 SurgePixel(); } }
technique ParticleBody { pass P0 { PixelShader = compile ps_3_0 ParticleBodyPixel(); } }
technique ParticleCrest { pass P0 { PixelShader = compile ps_3_0 ParticleCrestPixel(); } }
technique ParticleGlow { pass P0 { PixelShader = compile ps_3_0 ParticleGlowPixel(); } }
technique Disintegration { pass P0 { PixelShader = compile ps_3_0 DisintegrationPixel(); } }
technique PressureMuzzle { pass P0 { PixelShader = compile ps_3_0 PressureMuzzlePixel(); } }
