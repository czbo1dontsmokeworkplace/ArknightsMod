// W boss 爆炸冲击波：程序化扩张波前（谐波扰动 + 中心闪光），加法混合下绘制。
// 由 WExplosion.PreDraw 以实体着色器方式使用（镜像 LavaExplosionShaderEffect 的用法）。
sampler BaseTexture : register(s0);

float progress : register(c0);   // 0→1 冲击波扩张进度
float opacity : register(c1);    // 整体强度
float4 waveColor : register(c2); // 波体主色（加法混合）

float4 PixelShaderFunction(float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 p = texCoord - float2(0.5, 0.5);
    float dist = length(p) * 2.0;  // 中心 0，贴图边缘 1
    float angle = atan2(p.y, p.x);

    // 波前半径随进度扩张，波环随扩张变薄
    float radius = 0.12 + progress * 0.82;
    float width = lerp(0.17, 0.05, progress);

    // 两组谐波扰动波前，避免完美圆的塑料感
    float wobble = (sin(angle * 7.0 + progress * 9.0) + sin(angle * 13.0 - progress * 5.0)) * 0.022;

    float band = 1.0 - saturate(abs(dist - radius - wobble) / width);
    band = pow(band, 1.7);

    // 中心闪光：起爆瞬间最亮，随进度快速塌缩
    float core = saturate(1.0 - dist / max(radius, 0.05)) * pow(1.0 - progress, 2.0) * 0.8;

    float energy = (band + core) * opacity * (1.0 - progress);

    // 波前近白，向外衰减到主色
    float3 rgb = lerp(float3(1.0, 0.96, 0.88), waveColor.rgb, saturate(dist / max(radius, 0.05)));
    return float4(rgb * energy, 0.0); // alpha 为 0：纯加法发光，不压暗背景
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
