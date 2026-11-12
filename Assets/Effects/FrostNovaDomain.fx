// Independent ice-domain material: world-distance border, creeping whiteout and a visual landing wave.
float2 Anchor;
float Radius;
float Clock;
float Exposure;
float Opacity;
float Age;
struct Input { float3 Position : POSITION0; float2 World : TEXCOORD0; };
struct Surface { float4 Position : POSITION0; float2 World : TEXCOORD0; };
Surface VS(Input input) { Surface output; output.Position = float4(input.Position, 1); output.World = input.World; return output; }
float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float Noise(float2 p) {
    float2 cell = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
        lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
}
float4 Domain(Surface input) : COLOR0 {
    float2 p = input.World;
    float d = distance(p, Anchor);
    float cloud = Noise(p * .008 + float2(Clock * .13, -Clock * .08));
    cloud = cloud * .65 + Noise(p * .021 + float2(-Clock * .07, Clock * .16)) * .35;
    float outside = smoothstep(Radius - 4, Radius + 90, d);
    float edge = exp2(-abs(d - Radius) * .28);
    float frost = pow(saturate(1 - abs(sin(p.x * .024 + p.y * .031 + cloud * 7))), 16);
    float snow = pow(Noise(p * float2(.024, .13) + float2(Clock * -.5, Clock * .7)), 12);
    float alpha = outside * (.10 + Exposure * .77) * (.76 + cloud * .24);
    alpha += edge * (.35 + cloud * .25) + outside * frost * Exposure * .10;
    alpha = saturate(alpha * Opacity);
    float3 color = lerp(float3(.055, .14, .23), float3(.53, .76, .84), cloud);
    color = lerp(color, float3(.76, .94, 1), saturate(edge * .8 + frost * .3 + snow));
    return float4(color * alpha, alpha);
}
float4 Landing(Surface input) : COLOR0 {
    float d = distance(input.World, Anchor);
    float t = saturate(Age / 60);
    float front = 24 + 696 * (1 - pow(1 - t, 3));
    float width = lerp(34, 12, t);
    float band = exp2(-abs(d - front) / width * 3);
    float sharp = exp2(-abs(d - front - 4) * .55);
    float mist = exp2(-abs(d - front + 32) / 42) * .3;
    float noise = Noise(input.World * .035 - Clock * .24);
    float2 delta = input.World - Anchor;
    float angle = atan2(delta.y, delta.x);
    float streak = pow(saturate(cos(angle * 48 + noise)), 28)
        * smoothstep(front - 100, front - 30, d) * (1 - smoothstep(front + 10, front + 85, d));
    float burst = exp2(-d / 70) * exp2(-Age * .4);
    float alpha = saturate((band * .78 + sharp * .72 + mist * noise + streak * .4 + burst * .85)
        * pow(saturate(1 - t), .7) * Opacity);
    float3 color = lerp(float3(.18, .53, .73), float3(.86, .98, 1), saturate(sharp + band * .4 + streak + burst));
    return float4(color * alpha, alpha);
}
technique DomainPass { pass IceDomain { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 Domain(); } }
technique LandingPass { pass IceImpact { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 Landing(); } }
