// RT input at s0. Only UV displacement; no tint, light or RGB separation.
sampler uImage0 : register(s0);
float uTime;
float uOpacity;
float2 uViewportSize;
float2 uSourceSize;
float2 uOriginPixels;
float uStartAngle;
float uSweepAngle;
float uRotationSign;
float uRadiusPixels;
float uStrengthPixels;
float uShape; // 0 = swing sector, 1 = thrust capsule
float2 uEndPixels;
float uHalfWidthPixels;
float2 uImpactPixels;
float uImpactRadius;
float uImpactOpacity;

static const float TAU = 6.28318530718;

float4 HeatWavePS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 pixelPosition = uv * uViewportSize;
    float2 delta = pixelPosition - uOriginPixels;
    float radius = length(delta);
    float angle = atan2(delta.y, delta.x);
    float travel = frac((angle - uStartAngle) * uRotationSign / TAU) * TAU;

    // Pixel distances keep the circle round at any screen aspect ratio.
    float feather = max(1.0, min(18.0, uRadiusPixels * 0.15));
    float radialMask = 1.0 - smoothstep(max(0.0, uRadiusPixels - feather), uRadiusPixels, radius);
    float centerMask = smoothstep(0.0, feather, radius);
    float angularMask = 0.0;
    if (uSweepAngle >= TAU - 0.00001)
        angularMask = 1.0; // Full circles must not have an angle seam.
    else if (travel <= uSweepAngle && uSweepAngle > 0.0)
    {
        float edgeDistance = min(travel, uSweepAngle - travel) * radius;
        angularMask = smoothstep(0.0, feather, edgeDistance);
    }
    float mask = radialMask * centerMask * angularMask;
    if (uShape > 0.5)
    {
        float2 segment = uEndPixels - uOriginPixels;
        float t = saturate(dot(delta, segment) / max(dot(segment, segment), 0.0001));
        float distanceToAxis = length(delta - segment * t);
        mask = 1.0 - smoothstep(uHalfWidthPixels * 0.25, uHalfWidthPixels, distanceToAxis);
    }

    // Smooth moving waves, measured in pixels, advected in the requested direction.
    float2 flow = float2(cos(uStartAngle), sin(uStartAngle));
    float2 side = float2(-flow.y, flow.x) * uRotationSign;
    float along = dot(delta, flow);
    float across = dot(delta, side);
    float phase = across * 0.095 - uTime * 6.0;
    float waveA = sin(phase + sin(along * 0.04 + uTime * 2.2));
    float waveB = sin(along * 0.075 + across * 0.035 - uTime * 4.0);
    float2 offset = (side * waveA * 1.1 + flow * waveB * 0.32)
        * uStrengthPixels * mask * saturate(uOpacity) / uViewportSize;

    float2 impactDelta = pixelPosition - uImpactPixels;
    float impactDistance = length(impactDelta);
    float ringHalfWidth = max(2.0, uImpactRadius * 0.12);
    float ringMask = (1.0 - smoothstep(ringHalfWidth, ringHalfWidth + 2.0,
        abs(impactDistance - uImpactRadius))) * saturate(uImpactOpacity);
    float2 radial = impactDelta / max(impactDistance, 0.001);
    float ringWave = sin((impactDistance - uImpactRadius) * 0.28);
    offset += radial * ringWave * min(uStrengthPixels * 0.7, 18.0) * ringMask / uViewportSize;

    float2 halfPixel = 0.5 / uSourceSize;
    float2 sampleUV = clamp(uv + offset, halfPixel, 1.0 - halfPixel);
    return tex2D(uImage0, sampleUV);
}

technique HeatWaveTechnique
{
    pass HeatWave
    {
        PixelShader = compile ps_3_0 HeatWavePS();
    }
}
