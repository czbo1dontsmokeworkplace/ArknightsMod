// W 战天空：全程序化硝烟天穹。由 WBattleSky.Draw 按深度区间选 technique：
//   Far  —— 最远层（日月星辰之后、云背景与远山之前）：渐变天穹 + 域扭曲 fbm 硝烟云 + 地平线火光 + 爆炸照云 + 核爆余烬
//   Near —— 远山之后、物块之前：噪声暗幕 + 烟霭 + 落灰/余烬粒子 + 红霭 + 暗角 + 核爆白炽
// 铁律：零流控（无 if / 早 return / 动态循环），一切用 step/smoothstep/lerp 门控；[unroll] 静态循环会被完全展开。
// 输出预乘 alpha，在 BlendState.AlphaBlend 下同时做"压暗"（带 alpha）与"加光"（alpha 为 0）。
sampler uImage0 : register(s0); // SpriteBatch 绑定的 MagicPixel，仅为对齐仓库方言，不采样

float uTime : register(c0);       // Main.GlobalTimeWrappedHourly
float uIntensity : register(c1);  // 硝烟浓度 0~1（WBattleSky.intensity）
float uAspect : register(c2);     // 屏幕宽/高
float uRedPulse : register(c3);   // 低血量红光 0~1
float uNuke : register(c4);       // 核爆 0~1：>0.5 白炽段，<0.5 余烬段
float uNukeX : register(c5);      // 核爆屏幕 x 0~1
float2 uScroll : register(c6);    // 镜头位移（Main.screenPosition * 0.0005，各层乘自己的视差系数）
float uFlash : register(c7);      // 爆炸照亮云底 0~1
float uFlashX : register(c8);     // 照亮位置屏幕 x 0~1

// ---------- 噪声 ----------
float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.11369, 0.13787));
    p3 += dot(p3, p3.yzx + 19.19);
    return frac(float2((p3.x + p3.y) * p3.z, (p3.x + p3.z) * p3.y));
}

float vnoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// 八度之间旋转 ~37°，避免轴向条纹
static const float2x2 ROT = float2x2(0.80, 0.60, -0.60, 0.80);

float fbm2(float2 p)
{
    float v = vnoise(p) * 0.625;
    p = mul(p, ROT) * 2.03 + float2(3.7, 8.1);
    v += vnoise(p) * 0.375;
    return v;
}

float fbm3(float2 p)
{
    float v = vnoise(p) * 0.55;
    p = mul(p, ROT) * 2.07 + float2(3.7, 8.1);
    v += vnoise(p) * 0.30;
    p = mul(p, ROT) * 2.11 - float2(1.3, 5.5);
    v += vnoise(p) * 0.15;
    return v;
}

float fbm4(float2 p)
{
    float v = vnoise(p) * 0.50;
    p = mul(p, ROT) * 2.02 + float2(3.7, 8.1);
    v += vnoise(p) * 0.26;
    p = mul(p, ROT) * 2.09 - float2(1.3, 5.5);
    v += vnoise(p) * 0.15;
    p = mul(p, ROT) * 2.13 + float2(9.2, 2.4);
    v += vnoise(p) * 0.09;
    return v;
}

// 预乘 alpha 的 over 合成
void Over(inout float3 rgb, inout float a, float3 c, float ca)
{
    rgb = c * ca + rgb * (1.0 - ca);
    a = ca + a * (1.0 - ca);
}

// ---------- Far：硝烟天穹 ----------
float4 PSFar(float2 uv : TEXCOORD0) : COLOR0
{
    float t = uTime;
    float2 uvW = float2(uv.x * uAspect, uv.y);              // 等距坐标
    float2 camW = float2(uScroll.x * uAspect, uScroll.y);   // 镜头位移换到等距坐标
    float breath = 0.5 + 0.5 * sin(t * 2.4);                // 与 WBattleVisuals 滤镜同频呼吸
    float red = uRedPulse;
    float white = saturate((uNuke - 0.5) * 2.0);
    float after = saturate(uNuke * 2.0) * (1.0 - white);
    float horizon = smoothstep(0.30, 1.0, uv.y);

    // 1. 基础渐变：顶部近黑烟紫 → 地平线暖灰尘；低血量把地平线推向血红
    float3 col = lerp(float3(0.050, 0.040, 0.048), float3(0.115, 0.090, 0.092), saturate(uv.y * 1.6));
    col = lerp(col, float3(0.235, 0.160, 0.130), pow(saturate(uv.y), 2.2));
    col = lerp(col, float3(0.36, 0.09, 0.07), pow(saturate(uv.y), 1.8) * red * (0.55 + 0.45 * breath));

    // 2. 硝烟云：主层域扭曲 fbm（横向拉长的烟带）+ 次层细碎烟絮，浓度门限随硝烟浓度下降
    float2 p1 = (uvW + camW * 0.08) * float2(1.6, 2.6) + float2(t * 0.030, 0.0);
    float2 q = float2(fbm2(p1), fbm2(p1 + float2(5.2, 1.3)));
    float c1 = fbm4(p1 + (q - 0.5) * 0.9);
    float2 p2 = (uvW + camW * 0.14) * float2(3.2, 4.8) + float2(t * 0.018, 7.7);
    float c2 = fbm3(p2);
    float thr = lerp(0.62, 0.36, uIntensity);
    float cloud = saturate((c1 * 0.72 + c2 * 0.45 - thr) * 2.4);
    cloud = cloud * cloud * (3.0 - 2.0 * cloud);
    float emberAmt = saturate(horizon * (0.35 + 0.65 * red) + after * 0.8);
    float3 cloudCol = lerp(float3(0.030, 0.024, 0.028), float3(0.30, 0.10, 0.045), emberAmt);
    col = lerp(col, cloudCol, cloud * 0.88);

    // 3. 地平线火光：整条闪烁的火光 + 三处随视差缓移的地面火点（也照亮头顶的云底）
    float flick = 0.75 + 0.25 * vnoise(float2(uvW.x * 3.0 + t * 0.8, t * 1.7));
    float fireAmt = exp(-(1.0 - uv.y) * 4.5) * flick * (0.22 + 0.55 * red * (0.6 + 0.4 * breath)) * (0.4 + 0.6 * uIntensity);
    float spots = 0.0;
    [unroll]
    for (int i = 0; i < 3; i++)
    {
        float fi = (float)i;
        float sx = frac(hash21(float2(fi * 7.3, 1.7)) + 0.31 * fi - uScroll.x * 0.15) * 1.4 - 0.2;
        float dx = (uv.x - sx) * uAspect;
        float dy = (1.0 - uv.y) + 0.02;
        spots += exp(-dx * dx * 40.0 - dy * 7.0) * (0.7 + 0.3 * sin(t * (2.0 + fi) + fi * 3.1));
    }
    fireAmt += spots * (0.3 + 0.7 * uIntensity) * (0.35 + 0.5 * cloud);
    col += float3(0.95, 0.38, 0.12) * fireAmt;

    // 4. 爆炸照亮云底（uFlash/uFlashX）+ 远处炮火（每 2.3 秒掷一次，约一半概率闪一下）
    float cycle = t / 2.3;
    float k = floor(cycle);
    float ph = frac(cycle);
    float ambient = step(0.45, hash21(float2(k, 3.3))) * exp(-ph * 9.0) * 0.55;
    float ax = hash21(float2(k, 9.1));
    float dxa = (uv.x - uFlashX) * uAspect;
    float dxb = (uv.x - ax) * uAspect;
    float light = (uFlash * exp(-dxa * dxa * 1.6) + ambient * exp(-dxb * dxb * 2.2)) * lerp(0.45, 1.0, uv.y);
    col += float3(1.0, 0.72, 0.45) * light * (0.10 + 0.55 * cloud);

    // 5. 核爆：余烬段整片天穹转血橙 + 以 uNukeX 为中心的地平线光穹；白炽段先亮起来与 Near 层衔接
    float2 nd = float2((uv.x - uNukeX) * uAspect, (1.0 - uv.y) * 1.2);
    float dome = exp(-dot(nd, nd) * 2.2);
    col = lerp(col, float3(0.40, 0.12, 0.06), after * 0.6);
    col += float3(0.95, 0.42, 0.18) * after * (dome * 1.1 + cloud * 0.5);
    col = lerp(col, float3(1.0, 0.97, 0.92), white * 0.9);

    // 6. 细颗粒消色带
    col += (hash21(uv * float2(1731.0, 977.0) + frac(t) * 53.0) - 0.5) * 0.018;

    // 透明度跟硝烟浓度走：一阶段薄雾透出原版天色，濒死全遮；核爆期间强制压满
    float a = saturate(max(uIntensity * 1.12, uNuke * 1.5));
    return float4(saturate(col) * a, a);
}

// ---------- Near：近景烟霭与落灰 ----------
float4 PSNear(float2 uv : TEXCOORD0) : COLOR0
{
    float t = uTime;
    float2 uvW = float2(uv.x * uAspect, uv.y);
    float2 camW = float2(uScroll.x * uAspect, uScroll.y);
    float breath = 0.5 + 0.5 * sin(t * 2.4);
    float red = uRedPulse;
    float white = saturate((uNuke - 0.5) * 2.0);
    float after = saturate(uNuke * 2.0) * (1.0 - white);
    float horizon = smoothstep(0.30, 1.0, uv.y);

    float3 rgb = float3(0.0, 0.0, 0.0);
    float a = 0.0;
    float3 glow = float3(0.0, 0.0, 0.0);

    // 1. 暗幕：顶厚底薄，用低频噪声调制成不均匀的烟层
    float2 hp = (uvW + camW * 0.22) * float2(1.1, 1.7) + float2(t * 0.020, 0.0);
    float haze = fbm2(hp);
    float veilA = lerp(0.50, 0.12, uv.y) * uIntensity * (0.72 + 0.56 * haze);
    float3 veilCol = lerp(float3(0.085, 0.070, 0.078), float3(0.15, 0.05, 0.045), red * 0.45);
    Over(rgb, a, veilCol, saturate(veilA));

    // 2. 烟霭：大尺度烟团横飘，带明显镜头视差，低血量时靠地平线的烟被火光染红
    float2 wp = (uvW + camW * 0.35) * float2(0.9, 1.5) + float2(t * 0.035, t * 0.006 + 11.0);
    float wisp = smoothstep(0.42, 0.78, fbm3(wp));
    float3 wispCol = lerp(float3(0.06, 0.05, 0.055), float3(0.20, 0.06, 0.05), red * horizon * 0.6);
    Over(rgb, a, wispCol, wisp * 0.30 * uIntensity);

    // 3. 落灰三层：hash 网格粒子，近层更大更快视差更强；约两成是燃着的余烬（加法发光）
    float ashA = 0.0;
    float3 emberGlow = float3(0.0, 0.0, 0.0);
    float density = 0.10 + 0.16 * uIntensity;
    [unroll]
    for (int j = 0; j < 3; j++)
    {
        float fj = (float)j;
        float sc = 14.0 + fj * 12.0;
        float fall = 0.045 + fj * 0.030;
        float wind = 0.020 + fj * 0.012;
        float par = 0.45 + fj * 0.40;
        float2 g = ((uvW + camW * par) + float2(t * wind, -t * fall)) * sc;
        float2 id = floor(g);
        float2 sub = frac(g) - 0.5;
        float h = hash21(id + fj * 17.3);
        float2 o = (hash22(id + fj * 5.1 + 2.7) - 0.5) * 0.6;
        o.x += sin(t * (1.2 + h) + h * 6.28) * 0.10;
        float d = length(sub - o);
        float r = 0.045 + 0.05 * h;
        float vis = smoothstep(r, r * 0.35, d) * step(1.0 - density, frac(h * 7.31));
        float ember = step(0.80, frac(h * 13.7));
        ashA += vis * (1.0 - ember) * (0.35 + 0.25 * fj);
        float tw = 0.6 + 0.4 * sin(t * (3.0 + h * 5.0) + h * 20.0);
        emberGlow += float3(1.0, 0.42, 0.12) * vis * ember * tw;
    }
    Over(rgb, a, float3(0.14, 0.12, 0.13), saturate(ashA) * uIntensity);
    glow += emberGlow * (0.35 + 0.65 * uIntensity) * 0.9;

    // 4. 上升余烬：地面火场往上飘的火星，密度跟红光走
    {
        float2 g = ((uvW + camW * 0.6) + float2(t * 0.010, t * 0.070)) * 20.0;
        float2 id = floor(g);
        float2 sub = frac(g) - 0.5;
        float h = hash21(id + 31.7);
        float2 o = (hash22(id + 8.9) - 0.5) * 0.6;
        o.x += sin(t * 2.0 + h * 6.28) * 0.12;
        float spark = smoothstep(0.05, 0.0, length(sub - o));
        float dens = 0.30 * uIntensity + 0.70 * red;
        float gate = step(1.0 - 0.18 * dens, frac(h * 9.17));
        float tw = 0.5 + 0.5 * sin(t * (4.0 + h * 6.0) + h * 40.0);
        glow += float3(1.0, 0.50, 0.15) * spark * gate * tw * smoothstep(0.15, 0.9, uv.y) * 0.9;
    }

    // 5. 地平线红霭（加法）：低血量时底部一层呼吸的红光
    glow += float3(0.42, 0.08, 0.06) * pow(uv.y, 3.0) * red * (0.55 + 0.45 * breath) * 0.75;

    // 6. 暗角：硝烟越浓四周越压
    float2 vc = (uv - 0.5) * float2(1.0, 0.8);
    float vig = smoothstep(0.30, 0.85, length(vc) * 1.35);
    Over(rgb, a, float3(0.02, 0.015, 0.02), vig * 0.30 * uIntensity);

    // 7. 核爆：白炽段以 (uNukeX, 0.8) 为中心的径向白盖住远山，余烬段落成血红
    float2 nd = float2((uv.x - uNukeX) * uAspect, uv.y - 0.8);
    float nukeR = saturate(0.55 + 0.6 * exp(-dot(nd, nd) * 1.2));
    Over(rgb, a, float3(1.0, 0.98, 0.94), white * nukeR);
    Over(rgb, a, float3(0.62, 0.12, 0.10), after * 0.45);

    // 8. 细颗粒（随不透明度缩放，透明处不出噪点）
    rgb += (hash21(uv * float2(1731.0, 977.0) + frac(t) * 53.0) - 0.5) * 0.012 * a;

    return float4(saturate(rgb + glow), a);
}

technique Far
{
    pass P0
    {
        PixelShader = compile ps_3_0 PSFar();
    }
}

technique Near
{
    pass P0
    {
        PixelShader = compile ps_3_0 PSNear();
    }
}
