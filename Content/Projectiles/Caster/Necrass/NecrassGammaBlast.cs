using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

public sealed class NecrassGammaBlast : NecrassAttack
{
    private PhoenixMotionFx visual;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = Terraria.ModLoader.DamageClass.Magic;
        Projectile.penetrate = 1; Projectile.timeLeft = 360; Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (Main.dedServ) return;
        visual = new PhoenixMotionFx(73, Projectile, PhoenixFx.NecrassGamma());
        Vector2 at = Projectile.Center, axis = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        var style = PhoenixFx.NecrassGamma(muzzle: true);
        ReaperSwarmVisuals.Spawn("actor.player.weapon.73.muzzle", () => at, () => axis, .08f, scale: new Vector2(style.Scale), style: style);
        Vector2 center = Main.player[Projectile.owner].MountedCenter;
        ReaperSwarmVisuals.Spawn("actor.player.weapon.73.ship.effect", () => center, () => axis, .08f, scale: new Vector2(style.Scale), style: style);
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, .12f, .04f, .2f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 head = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 35;
        if (new Rectangle((int)head.X - 4, (int)head.Y - 4, 8, 8).Intersects(targetHitbox)) return true;
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            head - Projectile.velocity, head, 8, ref distance);
    }
    public override void OnKill(int timeLeft) => visual?.Stop();
    public override bool PreDraw(ref Color lightColor) => false;
}
