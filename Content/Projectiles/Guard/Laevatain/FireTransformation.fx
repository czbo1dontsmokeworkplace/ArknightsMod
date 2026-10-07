// Standalone player transformation fire. No textures or smoke.
// uTime/uDuration: seconds. uSize: relative width/height inside the drawn quad.
// Output is premultiplied for SpriteBatch + BlendState.AlphaBlend.
float uTime = 0.0;
float uDuration = 2.5;
float2 uSize = float2(1.0, 1.0);
float3 uFireColor = float3(1.0, 0.282353, 0.047059);

float FireHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float FireNoise(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(FireHash(cell), FireHash(cell + float2(1.0, 0.0)), f.x),
                lerp(FireHash(cell + float2(0.0, 1.0)), FireHash(cell + 1.0), f.x), f.y);
}

float FireFBM(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    // Four octaves keep the domain warp within the ps_3_0 instruction budget.
    [unroll]
    for (int octave = 0; octave < 4; octave++)
    {
        value += amplitude * FireNoise(p);
        p = float2(0.8 * p.x + 0.6 * p.y, -0.6 * p.x + 0.8 * p.y) * 2.02 + float2(3.1, 1.7);
        amplitude *= 0.55;
    }
    return value;
}

float4 FireTransformationPS(float2 texCoord : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float progress = saturate(uTime / max(uDuration, 0.001));
    // Anchor the base at the bottom center of the quad. Positive Y points upward.
    float2 uv = float2((texCoord.x - 0.5) * 2.0, 1.0 - texCoord.y) / max(uSize, 0.001);
    float height = uv.y;
    float t = uTime * 1.1;
    float2 domain = uv * float2(3.0, 4.8);
    domain.y -= t * 2.2;
    float warp = FireFBM(domain - t * 0.4);
    float n = FireFBM(domain + warp * 1.6);

    float width = lerp(0.85, 0.10, saturate(height));
    float body = saturate(1.0 - abs(uv.x) / width);
    float flame = saturate(body * (0.22 + 1.45 * n) - height * 0.55);
    flame *= flame;

    float ignition = smoothstep(0.0, 0.24, progress);
    float startMask = 1.0 - smoothstep(ignition, ignition + 0.12, height);
    // End emission at the base first; the remaining flame moves out through the top.
    float ending = saturate((progress - 0.68) / 0.32);
    float stopMask = smoothstep(ending * 1.18 - 0.12, ending * 1.18, height);
    float fadeOut = 1.0 - smoothstep(0.90, 1.0, progress);
    float bounds = (1.0 - smoothstep(0.84, 1.0, height)) *
                   (1.0 - smoothstep(0.80, 1.0, abs(uv.x)));
    float life = ignition * startMask * stopMask * fadeOut * bounds;

    float dark = smoothstep(0.02, 0.35, flame);
    float bright = smoothstep(0.24, 0.72, flame);
    float hot = smoothstep(0.65, 1.0, flame);
    float3 theme = saturate(uFireColor);
    float3 fireColor = theme * float3(0.65, 0.35, 0.25) * dark;
    fireColor += theme * bright;
    fireColor += sqrt(theme) * hot * 0.65;

    // Small sparks rise with their cells; no smoke contribution.
    float2 sparkUv = uv * float2(10.0, 12.0) - float2(0.0, t * 5.0);
    float cell = FireHash(floor(sparkUv));
    float2 local = frac(sparkUv) - 0.5;
    float spark = smoothstep(0.975, 1.0, cell) *
                  (1.0 - smoothstep(0.035, 0.13, length(local))) * body;

    float alpha = saturate(flame * 1.5 + spark) * life * vertexColor.a;
    float3 rgb = saturate(fireColor + sqrt(theme) * spark) * vertexColor.rgb;
    return float4(rgb * alpha, alpha);
}

technique FireTransformation
{
    pass FireTransformationPass
    {
        PixelShader = compile ps_3_0 FireTransformationPS();
    }
}
