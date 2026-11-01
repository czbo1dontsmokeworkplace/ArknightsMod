using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Caster.Ebenholz;
using ArknightsMod.Content.Projectiles.Caster.Indigo;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster;

public sealed class MysticPhotonBlast : ModProjectile
{
    internal const int Diameter = 300;
    private readonly HashSet<int> hitIdentities = new();
    private bool visualStarted;
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetStaticDefaults() => Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Type] = 300;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = Diameter;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => projHitbox.Intersects(targetHitbox);
    public override bool? CanHitNPC(NPC target)
    {
        int identity = EbenholzCombat.Identity(target);
        return hitIdentities.Contains(identity) || Projectile.ai[1] > 0 && identity == (int)Projectile.ai[1] - 1
            || !EbenholzCombat.Clear(Projectile.Center, target.Center) ? false : null;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitIdentities.Add(EbenholzCombat.Identity(target));
        if (Projectile.ai[0] == 0) IndigoTideSeal.TryApply(Projectile, target);
    }
    public override void AI()
    {
        if (visualStarted || Main.dedServ) return;
        visualStarted = true;
        // Exact source palette/curves/rings/core/sparks/impact audio. Visual
        // particles finish independently of the brief one-hit damage carrier.
        PhoenixFx.Burst("emitter.player.weapon.53.impact", Projectile.Center,
            Projectile.velocity.SafeNormalize(Vector2.UnitX));
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
