using System;
using ArknightsMod.Content.Projectiles.BossArsenal;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

public sealed class LapplandNoxShard : ModProjectile
{
    private PhoenixMotionFx visual;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 4;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        // Two updates per tick preserve the previous 40.8 px/tick launch speed
        // with a 20.4 px substep, and keep the small Nox head from skipping targets.
        Projectile.extraUpdates = 1; Projectile.timeLeft = 240;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        // Scalar acceleration only; neither a target search nor a steering angle.
        Projectile.velocity *= MathF.Pow(1.008f, 1f / Projectile.MaxUpdates);
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ) visual ??= NoxPreludeVisuals.Create(Projectile, true);
        LordLightingBolts.Flight(Projectile);
    }
    public override bool? Colliding(Rectangle a, Rectangle b)
    {
        float at = 0;
        return b.Intersects(a) || Collision.CheckAABBvLineCollision(b.TopLeft(), b.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, 14, ref at);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    { if (!Main.dedServ) NoxPreludeVisuals.Impact(Projectile, true); }
    public override void OnKill(int timeLeft) => visual?.Stop();
    public override bool PreDraw(ref Color lightColor)
    {
        BossArsenalPolishVisuals.IceMissile(Projectile, new Color(198, 211, 246));
        return false;
    }
}
