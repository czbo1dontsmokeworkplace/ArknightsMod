using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Lava;

public sealed class LavaBurst : ModProjectile
{
    private int Age => 18 - Projectile.timeLeft;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 120;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 18;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanDamage() => Age <= 2 ? null : false;
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<SpreadAfterfield>(),
                System.Math.Max(1, (int)(Projectile.damage * .28f)), Projectile.knockBack,
                Projectile.owner, 0f);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 closest = targetHitbox.ClosestPointInRect(Projectile.Center);
        return Vector2.DistanceSquared(closest, Projectile.Center) <= 60f * 60f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => target.AddBuff(BuffID.OnFire, 120);
    public override void AI()
    {
        if (Age != 0 || Main.dedServ) return;
        SpreadArtsVfx.Debris(Projectile.Center, SpreadArt.Ember, Projectile.width * .5f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        SpreadArtsVfx.Burst(Projectile.Center, Projectile.width, Age / 18f, SpreadArt.Ember);
        return false;
    }
}
