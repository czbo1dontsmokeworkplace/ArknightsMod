// 噪声图火焰，适用于 tModLoader 的 XNA/FNA 着色器。
// UV 左上为 (0, 0)，右下为 (1, 1)，火焰从底部向顶部流动。
// 只需一张可平铺的灰度噪声图，绑定到纹理槽 0。
sampler uImage0 : register(s0);

// 仅 VertexFire 技术需要该矩阵；SpriteFire 使用 SpriteBatch 自带的顶点变换。
float4x4 uTransform;

// 从本次火焰开始起算的秒数，停止生成后仍需继续递增。
float uTime = 0.0;
// 停止生成的相对秒数；负数表示持续生成。
float uStopTime = 2.0;
// 每秒上升的绘制区域高度，必须大于零；停止后最多再运行 1 / uFlowSpeed 秒。
float uFlowSpeed = 0.65;
// 噪声重复次数：X 越大火舌越细，Y 越小火舌越长。
float2 uNoiseScale = float2(3.0, 1.4);
// 每团火焰可给不同的固定偏移，避免所有火焰一模一样。
float2 uNoiseOffset = float2(0.0, 0.0);
// 噪声坐标的横向扭曲强度。
float uDistortion = 0.18;
// 灰度阈值越低，产生火焰的区域越大。
float uThreshold = 0.32;
// 火舌轮廓的柔边宽度，不是生命周期的淡出值。
float uEdge = 0.045;
// 火焰横向宽度比例，通常取 0.5 到 1.0。
float uWidth = 0.92;

// 灰度由低到高对应外焰、中焰、内焰的颜色。
float3 uOuterColor = float3(0.65, 0.035, 0.005);
float3 uMiddleColor = float3(1.0, 0.30, 0.015);
float3 uInnerColor = float3(1.0, 0.94, 0.50);

struct VertexInput
{
    float2 Position : POSITION0;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

VertexOutput FireVS(VertexInput input)
{
    VertexOutput output;
    output.Position = mul(float4(input.Position, 0.0, 1.0), uTransform);
    output.Color = input.Color;
    output.UV = input.UV;
    return output;
}

float4 FirePS(float2 uv : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float speed = max(uFlowSpeed, 0.0001);
    float time = max(uTime, 0.0);

    // 对当前像素反推它经过底部发射口的时间。
    // 只有出生时间在 [0, uStopTime) 内的火焰才存在。
    float birthTime = time - (1.0 - uv.y) / speed;
    float emitted = step(0.0, birthTime);
    if (uStopTime >= 0.0)
        emitted *= 1.0 - step(uStopTime, birthTime);

    // UV 的 Y 轴向下：增加采样 Y，画面中的噪声特征就会向上移动。
    // 必须先平移再乘噪声缩放，保证火焰和停火边界的上升速度一致。
    float2 materialUV = float2(uv.x, uv.y + time * speed);
    float2 noiseUV = materialUV * uNoiseScale + uNoiseOffset;

    // 扭曲与细节也只使用随流动保持不变的坐标。
    // 停火后继续推进 time，已有火舌的形状与颜色会继续向上输送。
    float bend = tex2D(uImage0, noiseUV * 0.47 + float2(0.13, 0.71)).r;
    noiseUV.x += (bend * 2.0 - 1.0) * uDistortion;
    float largeNoise = tex2D(uImage0, noiseUV).r;
    float smallNoise = tex2D(uImage0, noiseUV * 2.03 + float2(0.37, 0.19)).r;
    float gray = largeNoise * 0.8 + smallNoise * 0.2;

    // 两侧提高灰度门槛，噪声越亮，火舌横向能延伸得越远。
    // 不叠加随时间变化的整体亮度或透明度，保留剩余火焰的颜色。
    float side = abs(uv.x * 2.0 - 1.0) / max(uWidth, 0.001);
    float threshold = saturate(uThreshold) + side * side * 0.65;
    float edge = max(uEdge, 0.0001);
    float shape = smoothstep(threshold, threshold + edge, gray);
    float heat = saturate((gray - threshold) / max(1.0 - threshold, 0.0001));

    float3 fireColor = lerp(uOuterColor, uMiddleColor, smoothstep(0.0, 0.38, heat));
    fireColor = lerp(fireColor, uInnerColor, smoothstep(0.38, 0.85, heat));

    // Alpha 只由火焰轮廓、出生区间和外部顶点颜色决定。
    // 输出普通 Alpha，配合 BlendState.AlphaBlend。
    // 顶点颜色的 RGB 保持原色，Alpha 只控制透明度。
    float coverage = shape * emitted;
    return float4(fireColor * vertexColor.rgb, vertexColor.a * coverage);
}

// DrawUserPrimitives 使用此技术，手动设置 uTransform。
technique VertexFire
{
    pass FirePass
    {
        VertexShader = compile vs_3_0 FireVS();
        PixelShader = compile ps_3_0 FirePS();
    }
}

// SpriteBatch.Draw 使用此技术，保留 SpriteBatch 自带的顶点着色器。
technique SpriteFire
{
    pass FirePass
    {
        PixelShader = compile ps_3_0 FirePS();
    }
}
