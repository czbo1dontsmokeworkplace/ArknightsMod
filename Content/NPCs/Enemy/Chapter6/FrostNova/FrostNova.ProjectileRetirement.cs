using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public sealed partial class FrostNovaOwnedProjectile
{
    internal const int RetirementTicks = 36;
    internal int RetirementAge = -1;
    internal bool Retiring => RetirementAge >= 0;
    internal float Visibility => Retiring ? 1 - MathHelper.SmoothStep(0, 1, Math.Clamp(RetirementAge / (float)RetirementTicks, 0, 1)) : 1;
    private static void Disarm(Projectile p) { p.hostile = p.friendly = false; p.damage = 0; p.tileCollide = false; }
    internal void Retire(Projectile p)
    {
        if (Retiring || !p.active) return;
        Disarm(p); RetirementAge = 0;
        p.timeLeft = (RetirementTicks + 2) * p.MaxUpdates;
        if (Main.netMode != NetmodeID.MultiplayerClient) p.netUpdate = true;
    }
    private bool RetirementDue(Projectile p)
    {
        // These two already fade smoothly to zero in their own natural lifetime.
        if (p.ModProjectile is FrostNovaBlackIceRegion or SnowSupportPortal) return false;
        return p.timeLeft <= p.MaxUpdates
            || p.ModProjectile is BlackIceIntro && p.frame >= 13 && p.frameCounter >= 2
            || p.ModProjectile is BlackIceExplode && p.frame >= 4 && p.frameCounter >= 5;
    }
    private void UpdateRetirement(Projectile p)
    {
        Disarm(p);
        if (p.numUpdates == 0) { RetirementAge++; p.velocity *= .98f; }
        if (p.ModProjectile is FrostNovaBarrage && p.ai[1] != 2) p.position += p.velocity / p.MaxUpdates;
        p.Opacity = Visibility;
        if (RetirementAge >= RetirementTicks) p.active = false; // Never run expiry explosions or child spawns.
    }
    public override bool? CanDamage(Projectile projectile) => Token != 0 && Retiring ? false : null;
    public override bool PreKill(Projectile projectile, int timeLeft) => Token == 0 || !Retiring;
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (Token == 0 || !FrostNovaProjectileArt.Supports(projectile)) return true;
        FrostNovaProjectileArt.Draw(projectile, Visibility);
        return false;
    }
}
