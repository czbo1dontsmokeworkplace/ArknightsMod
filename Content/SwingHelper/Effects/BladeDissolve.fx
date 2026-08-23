// Blade dissolve shader. uDissolve: 0 = intact, 1 = fully dissolved.
sampler uImage0 : register(s0);
sampler uNoiseTex : register(s1);
float4x4 uTransform;
float uTime;
float uDissolve;
float uNoiseScale;

struct VSInput
{
    float2 Pos : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VS(VSInput input)
{
    VSOutput o;
    o.Pos = mul(float4(input.Pos, 0, 1), uTransform);
    o.Color = input.Color;
    o.TexCoord = input.TexCoord;
    return o;
}

float4 PS(VSOutput input) : COLOR0
{
    float4 c = tex2D(uImage0, input.TexCoord);
    float2 uv = input.TexCoord;

    // Broad noise patches with a finer breakup layer and slow drift.
    float2 drift = float2(uTime * 0.01, uTime * 0.006);
    float field = tex2D(uNoiseTex, uv * uNoiseScale + drift).r;
    float detail = tex2D(uNoiseTex, uv * uNoiseScale * 3.0 + float2(0.37, 0.19)).r;
    field = field * 0.75 + detail * 0.25;

    float threshold = uDissolve;
    threshold = max(threshold, 0.0);
    threshold = min(threshold, 1.0);
    float width = 0.08;
    float alive = smoothstep(threshold - width, threshold + width, field);

    // A narrow glow band marks the active dissolve front.
    float edge = smoothstep(threshold - width, threshold, field)
               * (1.0 - smoothstep(threshold, threshold + width, field));
    c.rgb = c.rgb * alive + float3(0.6, 0.9, 1.2) * edge * 0.75;

    // Keep the source texture alpha for soft blade edges.
    c.a *= max(alive, edge * 0.35);
    return c * input.Color;
}

technique Technique1
{
    pass Base
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}
