// Local command-display material: metric grid, square data packets and a sweeping scan front.
// Shader visuals are decorative; readable attack limits are always drawn without this effect.
float uTime;
float2 uSize;
float uReveal;
float4 uUvBounds;
texture uImage;
sampler uTexture : register(s0) = sampler_state { Texture = <uImage>; };

float4 HullPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(uTexture, uv);
    float2 local = (uv - uUvBounds.xy) / max(uUvBounds.zw, .001);
    float scan = pow(saturate(1.0 - abs(local.y - frac(uTime * .28)) * 28.0), 2.0);
    float rows = .55 + .45 * step(.38, frac(local.y * uSize.y * .25));
    float reveal = 1.0 - smoothstep(uReveal - .025, uReveal + .025, local.y);
    float rim = saturate(1.0 - abs(local.y - uReveal) * 25.0);
    float3 rgb = source.rgb * .35 + tint.rgb * (rows * .6 + rim * 1.2 + scan * .6);
    return float4(rgb * source.a * tint.a * reveal, source.a * tint.a * reveal * .65);
}
float4 TechPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 p = uv * uSize;
    float scan = pow(saturate(1.0 - abs(uv.y - frac(uTime * .28)) * 28.0), 2.0);
    float rows = .55 + .45 * step(.38, frac(p.y * .25));
    float2 cell = frac(p / 24.0);
    float grid = max(1.0 - step(.035, cell.x), 1.0 - step(.035, cell.y));
    float edge = min(min(p.x, uSize.x - p.x), min(p.y, uSize.y - p.y));
    float border = 1.0 - smoothstep(1.0, 2.5, edge);
    float2 id = floor(p / 24.0);
    float packet = step(.96, frac(sin(dot(id, float2(12.989, 78.233)) + floor(uTime * 7.0)) * 43758.54));
    packet *= step(.18, cell.x) * step(cell.x, .44) * step(.18, cell.y) * step(cell.y, .44);
    float intensity = grid * .12 + scan * .48 + border * .6 + packet * .7;
    float alpha = saturate(.025 + intensity * .42) * tint.a;
    float3 color = tint.rgb * (.045 + intensity * rows);
    return float4(color * tint.a, alpha);
}
technique MaterialistTech
{
    pass ScanPass { PixelShader = compile ps_3_0 TechPixel(); }
}
technique MaterialistHull
{
    pass HullPass { PixelShader = compile ps_3_0 HullPixel(); }
}

// Visual reference: Calamity Mod MiracleBlight.fx (Azafure, LLC),
// https://github.com/CalamityTeam/CalamityModPublic . Independently implemented
// procedural flowing threshold/rim: no imported shader binary, noise texture or debuff.
float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float Noise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
}
float4 OverclockPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(uTexture, uv);
    float2 local = (uv - uUvBounds.xy) / max(uUvBounds.zw, .001);
    float2 p = local * float2(7.0, 4.0) + float2(sin(uTime * .47) * .8, uTime * .7);
    float flow = Noise(p) * .7 + Noise(p * 2.1 - uTime * .3) * .3;
    float threshold = .51 + .035 * cos(uTime * 7) + .025 * sin(uTime * 31);
    float rim = 1.0 - smoothstep(.015, .075, abs(flow - threshold));
    float scan = .6 + .4 * step(.45, frac(local.y * uSize.y * .32 + uTime * 2));
    float3 color = lerp(float3(.08, .85, 1), float3(.65, 1, .1), saturate(flow * 1.8 - .3));
    color = lerp(color, float3(1, .3, .07), saturate(flow * 3 - 1.8));
    float alpha = source.a * tint.a * (.1 + rim * .65);
    return float4((source.rgb * .25 + color * (rim * 1.6 + .18) * scan) * alpha, alpha);
}
technique MaterialistOverclock
{
    pass OverclockPass { PixelShader = compile ps_3_0 OverclockPixel(); }
}
// Layered chromatic streaks inspired by Heavenly Gale's broad energy trail.
float4 EnergyPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float n = Noise(uv * float2(10, 4) - float2(uTime * 3.4, uTime));
    float core = pow(saturate(1 - abs(uv.y - .5) * 2), 2.5);
    float wave = .65 + .35 * sin(uv.x * 34 - uTime * 18 + n * 6);
    float alpha = core * tint.a * (.35 + n * .65);
    float3 rgb = lerp(tint.rgb, float3(.85, 1, .5), n * .6) * (wave + core * .5);
    return float4(rgb * alpha, alpha);
}
technique MaterialistEnergy
{
    pass EnergyPass { PixelShader = compile ps_3_0 EnergyPixel(); }
}
float4 OutlinePixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 d = 1.5 / uSize;
    float center = tex2D(uTexture, uv).a;
    float rim = max(max(tex2D(uTexture, uv + float2(d.x, 0)).a, tex2D(uTexture, uv - float2(d.x, 0)).a),
                    max(tex2D(uTexture, uv + float2(0, d.y)).a, tex2D(uTexture, uv - float2(0, d.y)).a));
    rim = max(rim, max(tex2D(uTexture, uv + d).a, tex2D(uTexture, uv - d).a));
    float alpha = saturate(rim - center) * tint.a;
    return float4(tint.rgb * alpha * 1.4, alpha);
}
technique MaterialistOutline
{
    pass OutlinePass { PixelShader = compile ps_3_0 OutlinePixel(); }
}

// Native Moon Lord start/body/end sprites already carry their edge falloff and animation.
// Extract premultiplied light rather than multiply cyan RGB by purple RGB.
float4 LaserPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(uTexture, uv);
    float light = max(source.r, max(source.g, source.b));
    return float4(tint.rgb * light, 0);
}
technique MaterialistLaser
{
    pass LaserPass { PixelShader = compile ps_3_0 LaserPixel(); }
}

// Phase-wait envelope: curved shell, metric lattice and a travelling assembly/disassembly front.
float4 LockShieldPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 local = uv * 2.0 - 1.0;
    float radius = length(local);
    float shell = 1.0 - smoothstep(.96, 1.0, radius);
    float rim = pow(saturate(1.0 - abs(radius - .955) * 32.0), 1.5);
    float reveal = 1.0 - smoothstep(uReveal - .025, uReveal + .025, uv.y);
    float front = pow(saturate(1.0 - abs(uv.y - uReveal) * 28.0), 2.0);
    float2 cell = frac(uv * uSize / float2(26.0, 20.0));
    float grid = max(1.0 - step(.035, cell.x), 1.0 - step(.045, cell.y));
    float scan = pow(saturate(1.0 - abs(uv.y - frac(uTime * .23)) * 24.0), 3.0);
    float rows = .65 + .35 * step(.45, frac(uv.y * uSize.y * .28 + uTime * 1.2));
    float energy = .018 + grid * .16 + rim * .55 + front * .6 + scan * .22;
    float alpha = saturate(energy * shell * reveal * tint.a);
    float3 rgb = lerp(tint.rgb, float3(.8, .95, 1.0), saturate(rim * .45 + front * .3));
    return float4(rgb * alpha * rows, alpha * .65);
}
technique MaterialistLockShield
{
    pass ShieldPass { PixelShader = compile ps_3_0 LockShieldPixel(); }
}

// Carrier destruction: cell-addressed scan failure and spectral edges follow the real chassis alpha.
float4 DisassemblyPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 local = (uv - uUvBounds.xy) / max(uUvBounds.zw, .001);
    float2 cell = floor(local * float2(28.0, 14.0));
    float fault = Hash(cell + floor(uTime * 18.0));
    float threshold = Hash(cell);
    float intact = 1.0 - smoothstep(threshold + .20, threshold + .32, uReveal);
    float scan = pow(saturate(1.0 - abs(frac(local.y * 8.0 - uTime * 2.5) - .5) * 2.0), 10.0);
    float shift = (fault - .5) * .04 * uReveal;
    float4 source = tex2D(uTexture, uv + float2(shift, 0));
    float rows = .45 + .55 * step(.4, frac(local.y * uSize.y * .35));
    float edge = abs(tex2D(uTexture, uv + float2(.007, 0)).a - tex2D(uTexture, uv - float2(.007, 0)).a);
    float3 spectral = lerp(float3(.1, .9, 1), float3(1, .28, .08), step(.55, fault));
    float alpha = source.a * intact * tint.a;
    float3 color = source.rgb * .2 + spectral * (scan * .6 + rows * .25 + edge * 1.5);
    return float4(color * alpha, alpha * .55);
}
technique MaterialistDisassembly
{
    pass DisassemblyPass { PixelShader = compile ps_3_0 DisassemblyPixel(); }
}

// Photonic cuts have a tapered luminous blade and a trailing broken packet sheath.
float4 SeverancePixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float x = uv.x * 2.0 - 1.0;
    float taper = pow(saturate(1.0 - abs(x)), .6);
    float bend = sin(uv.x * 3.14159265) * (.18 - uReveal * .15);
    float across = abs(uv.y - .5 + bend);
    float blade = exp(-across * 110.0 / max(.2, taper));
    float halo = exp(-across * 14.0) * taper;
    float2 cell = floor(uv * uSize / float2(13, 4));
    float packets = step(.65, Hash(cell + floor(uTime * 18))) * halo * .35;
    float spine = blade * (1.0 - smoothstep(.88, 1.0, abs(x)));
    float3 color = tint.rgb * (halo * .55 + packets) + lerp(tint.rgb, float3(1, 1, 1), .88) * spine * 1.4;
    return float4(color * tint.a, 0);
}
technique MaterialistSeverance
{
    pass SeverancePass { PixelShader = compile ps_3_0 SeverancePixel(); }
}
