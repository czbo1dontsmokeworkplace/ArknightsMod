// Original Evolution weapon material. Existing mod noise only, no external shader or artwork.
float uTime;
float uOpacity;
float uLength;
float uKind;
float uSeed;
float uOffset;
float uSpan;
sampler uNoise : register(s1);

float4 OrganicPixel(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float t = uTime + uSeed * .37;
    float3 red = float3(.88, .035, .115);
    float3 ivory = float3(1, .76, .57);
    float2 p = uv * 2 - 1;
    float n = tex2D(uNoise, uv * 2.3 + float2(-t * .37, t * .13)).r;
    float n2 = tex2D(uNoise, uv * 5.7 + float2(t * .2, -t * .21)).r;
    float3 rgb;
    float alpha = 0;
    if (uKind < .5)
    {
        float r = length(p);
        float a = atan2(p.y, p.x);
        float pulse = .035 * sin(t * 8.5 + uSeed);
        float rim = exp(-pow((r - .56 - pulse - (n - .5) * .09) * 16, 2));
        float corona = pow(saturate(1 - r), 2.8);
        float heart = exp(-dot(p + float2(sin(t * 1.4), cos(t * 1.8)) * .07, p) * 19);
        float cracks = pow(saturate(sin(a * 6 + r * 14 - t * 2.8 + n * 5)), 14) * exp(-r * r * 4);
        float cells = pow(saturate(cos(a * 3 - r * 17 + t * 2 + n2 * 3)), 9) * saturate(1 - r * 1.4);
        rgb = red * (corona * .8 + rim * .75 + cells * .28) + ivory * (heart * .9 + cracks * .75 + rim * .12);
        rgb *= smoothstep(1, .8, r);
    }
    else if (uKind < 1.5)
    {
        float x = uOffset + uv.x * uSpan;
        float noise = tex2D(uNoise, float2(x * uLength / 160 - t * 1.6, uv.y * 2.8 + uSeed)).r;
        float flow = sin(x * uLength * .044 - t * 13 + noise * 4) * .08;
        float y = p.y - flow;
        float mantle = exp(-y * y * 8) * (.5 + noise * .65);
        float spine = exp(-y * y * 100) * (.7 + n2 * .3);
        float veins = exp(-pow((abs(y) - .25 - sin(x * 19 + t * 4) * .08) * 20, 2));
        float edge = pow(saturate(1 - abs(y) - (noise - .5) * .14), 3);
        float ends = smoothstep(0, .025, x) * smoothstep(0, .04, 1 - x);
        rgb = (red * (mantle * .6 + edge * .3) + ivory * (spine * .8 + veins * .13)) * ends;
    }
    else if (uKind < 2.5)
    {
        float body = pow(saturate(1 - length(p)), 1.7) * (.3 + n * .7);
        float filaments = pow(saturate(sin(p.x * 9 + p.y * 6 + n2 * 8 - t * 1.6)), 12);
        alpha = body * .3;
        rgb = red * body * (.1 + filaments * .3);
    }
    else if (uKind < 3.5)
    {
        // Confined double-helical veins, drifting cells and an ivory pulse spine.
        // x remains continuous over every curved segment of this projectile's history.
        float x = uOffset + uv.x * uSpan;
        float cells = tex2D(uNoise, float2(x * 8 - t * 3.1, uv.y * 2 + uSeed)).r;
        float warp = sin(x * 22 - t * 17 + cells * 3) * .09;
        float y = p.y - warp;
        float envelope = smoothstep(0, .035, 1-x);
        float pulse = .65 + .35 * pow(saturate(sin(x * 25 + t * 18)), 5);
        float shell = exp(-y*y*8) * (.25 + cells * .4);
        float spine = exp(-y*y*155) * pulse;
        float helix = sin(x * 30 - t * 15 + uSeed) * .42;
        float veins = exp(-pow((y-helix)*20,2)) + exp(-pow((y+helix)*20,2));
        float seams = pow(saturate(sin(x*54-t*11+cells*3)),9) * exp(-y*y*11);
        rgb = (red*(shell+veins*.3+seams*.23)+ivory*(spine*.86+veins*.16)) * envelope;
    }
    else if (uKind < 4.5)
    {
        // A pointed crystalline seed with a split red shell, rather than a glowing ball.
        float blade = saturate(1-abs(p.y)/max(.025,(1-p.x)*.31));
        float silhouette = smoothstep(-.95,-.7,p.x) * smoothstep(1,.82,p.x);
        float shell = pow(blade,2) * silhouette;
        float heart = exp(-p.y*p.y*230) * smoothstep(-.5,-.2,p.x) * smoothstep(1,.7,p.x);
        float split = exp(-pow((abs(p.y)-(.2-.1*p.x))*30,2)) * smoothstep(-.85,-.5,p.x) * smoothstep(.65,.35,p.x);
        float fractures = pow(saturate(sin(p.x*24+abs(p.y)*18-t*9+n*5)),10) * shell;
        rgb = red*(shell*.85+split*.6)+ivory*(heart*.95+fractures*.24);
    }
    else if (uKind < 5.5)
    {
        // A fast severing sheet: broken red films, narrow pale edges and short arterial filaments.
        float x = uOffset + uv.x * uSpan;
        float cutNoise = tex2D(uNoise, float2(x * uLength / 80 - t * 6, uv.y * 3 + uSeed)).r;
        float y = p.y;
        float edge = exp(-pow((y + .22 + sin(x * 24 - t * 36) * .04) * 24, 2));
        float spine = exp(-y*y*135) * (.75 + cutNoise * .25);
        float sheet = pow(saturate(1-abs(y)*1.4),2) * (.2+cutNoise*.6);
        float tears = pow(saturate(sin(x*58-t*42+cutNoise*5)),9) * exp(-y*y*7);
        float veins = exp(-pow((abs(y)-.38-cutNoise*.12)*21,2));
        float ends = smoothstep(0,.025,x) * smoothstep(0,.025,1-x);
        rgb = (red*(sheet*.85+tears*.65+veins*.3)+float3(1,.72,.79)*(edge*.85+spine*.7))*ends;
    }
    else
    {
        // Crimson suspended crystal powder: soft cellular billows with sparse angular grains.
        float r = length(p);
        float a = atan2(p.y,p.x);
        float lobes = .055*sin(a*5+t*1.7+uSeed)+.035*sin(a*9-t*.8);
        float rim = smoothstep(1,.65,r+lobes+(n-.5)*.13);
        float flow = tex2D(uNoise,uv*2.8+float2(-t*.45+uSeed*.1,t*.18)).r;
        float cells = tex2D(uNoise,uv*6.4+float2(t*.23,-t*.34)).r;
        float density = rim*(.16+flow*.5+cells*.18);
        float2 grainUV = frac(uv*18+float2(uSeed*.17,t*.035))-.5;
        float grains = pow(saturate(1-(abs(grainUV.x)+abs(grainUV.y))*5),3);
        float selection = smoothstep(.56,.82,n2);
        float veins = pow(saturate(sin(p.x*17+p.y*11+flow*8-t*2)),13)*rim;
        alpha = density*.28;
        rgb = float3(.26,.012,.042)*alpha + red*(density*.3+veins*.09)
            +float3(1,.48,.62)*grains*selection*rim*.6;
    }
    return float4(rgb * uOpacity * tint.rgb * tint.a, alpha * uOpacity * tint.a);
}
technique EvolutionOrganicWeapons
{
    pass OrganicPass { PixelShader = compile ps_3_0 OrganicPixel(); }
}
