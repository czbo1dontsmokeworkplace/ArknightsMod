using ArknightsMod.Content.Projectiles.Caster.Leizi;
using ArknightsMod.Content.Projectiles.Caster.Passenger;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Pudding;

/// <summary>保留惊蛰的实体命中与异客的确定性选敌；飞行和弹跳使用布丁独立的视觉入口。</summary>
public sealed class PuddingFlyingDischarge : LeiziFlyingDischarge
{
    internal override LightningPalette Palette => LightningPalette.Pudding;
    internal override float VisualSize => 0.72f;
    internal override int ChainProjectileType => ModContent.ProjectileType<PuddingChainDischarge>();
    internal override int ChainTargetLimit => ChainArtsVfx.TargetCount;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 20;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    protected override void EmitFlightVisuals(Vector2 start, Vector2 end, float scale, int phase)
    {
        ChainArtsVfx.Flight(start, end, scale, phase, Projectile.identity * 397 + phase * 13, Palette, 3);
        PuddingVisuals.Flight(start, end, scale, phase);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ChainArtsVfx.Head(Projectile, Palette, 3);
        return false;
    }
}

public sealed class PuddingChainDischarge : PassengerChainDischarge
{
    internal override LightningPalette Palette => LightningPalette.Pudding;
    protected override float VisualSize => 0.72f;
    protected override bool AllowRepeatTargets => Projectile.ai[0] >= 1.39f;

    protected override void BeginHopVisuals(Vector2 start, Vector2 end, float scale,
        float nodeIntensity, int seed, int hop, bool targetNode)
    {
        ChainArtsVfx.Hop(start, end, scale, nodeIntensity, seed, 3, Palette, targetNode);
        PuddingVisuals.BeginHop(start, end, scale, seed, targetNode);
        if (targetNode)
            PassengerLightningVisuals.PrepareImpact(end, nodeIntensity, seed, Palette);
        if (hop > 0 && targetNode)
            PassengerLightningVisuals.JumpAccent(start, end, nodeIntensity, seed, Palette);
    }

    protected override void AdvanceHopVisuals(Vector2 start, Vector2 end, float scale, int phase)
    {
        PassengerLightningVisuals.EmitSpan(start, end, scale, phase, Palette);
        PuddingVisuals.AdvanceHop(start, end, scale, phase);
    }

    protected override void ImpactHopVisuals(Vector2 end, float intensity, int seed,
        bool firstHop, bool collided)
    {
        ChainArtsVfx.Impact(end, intensity, 3, Palette);
        PassengerLightningVisuals.Burst(end, intensity * .65f, seed, Palette);
        PuddingVisuals.Impact(end, intensity, seed, firstHop);
        if (collided)
            PassengerLightningVisuals.ElectricImpactDust(end, intensity, firstHop, Palette);
    }
}
