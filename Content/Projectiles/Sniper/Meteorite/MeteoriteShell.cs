using System;
using ArknightsMod.Content.Projectiles.Sniper.Fiammetta;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Meteorite;

// Share the ascent, sky transfer, constrained descent and synchronized target lock.
public sealed class MeteoriteShell : FiammettaMortarShell
{
    private int Mode => Math.Clamp((int)Projectile.ai[2], 0, 2);
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ElectrosphereMissile;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MeteoriteShellVisuals.DrawTrail(Projectile);
        return true;
    }
    protected override void SpawnExplosion(int damage)
    {
        int burstDamage = Math.Max(1, (int)(damage * (Mode == 2 ? .9f : .68f)));
        Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<MeteoriteBurst>(), burstDamage,
            Projectile.knockBack, Projectile.owner, Mode);
        int count = Mode == 2 ? 8 : Mode == 1 ? 7 : 5;
        float phase = Main.rand.NextFloat(MathHelper.TwoPi);
        for (int i = 0; i < count; i++)
        {
            Vector2 fragmentVelocity = (phase + MathHelper.TwoPi * i / count).ToRotationVector2()
                * Main.rand.NextFloat(5.5f, 8f);
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center,
                fragmentVelocity, ModContent.ProjectileType<MeteoriteFragment>(),
                Math.Max(1, (int)(Projectile.damage * .24f)),
                Projectile.knockBack * .35f, Projectile.owner);
        }
    }
}
