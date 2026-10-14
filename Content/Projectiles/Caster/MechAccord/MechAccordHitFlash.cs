using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

public sealed class MechAccordHitFlash : ModProjectile
{
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    private bool emitted;
    internal static void Spawn(Projectile source, Vector2 point, bool rockrock, float diameter)
    {
        if (source.owner != Main.myPlayer) return;
        int type = ModContent.ProjectileType<MechAccordHitFlash>();
        if (Main.player[source.owner].ownedProjectileCounts[type] >= 48) return;
        Projectile.NewProjectile(source.GetSource_FromThis(), point, Vector2.Zero, type, 0, 0f,
            source.owner, rockrock ? 1f : 0f, Math.Clamp(diameter, 24f, 72f));
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 18;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!emitted) { emitted = true; MechAccordVisuals.Emit(Projectile.Center, Projectile.ai[0] > 0); }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MechAccordVfx.DrawBurst(Projectile.Center, (18f - Projectile.timeLeft) / 18f,
            Projectile.ai[0] > 0, Math.Clamp(Projectile.ai[1], 24f, 72f));
        return false;
    }
}
