using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

// Invisible damage carrier, rendered entirely with particles. Does not spawn more sparks.
public sealed class VanguardSpearSpark : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public static void Release(Projectile source, Vector2 point, VanguardSpearKind kind, bool heavy)
    {
        if (source.owner != Main.myPlayer) return;
        int type = ModContent.ProjectileType<VanguardSpearSpark>();
        Player owner = Main.player[source.owner];
        int count = Math.Min(heavy ? 6 + (int)kind : Main.rand.Next(1, 4), 36 - owner.ownedProjectileCounts[type]);
        Vector2 aim = source.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = VanguardSpearRules.SparkLaunchVelocity(aim, owner.gravDir, heavy,
                Main.rand.NextFloat(), Main.rand.NextFloat(), Main.rand.NextFloat());
            Projectile p = Projectile.NewProjectileDirect(source.GetSource_FromThis(), point, velocity,
                type, Math.Max(1, (int)(source.damage * (heavy ? .1f : .18f))), source.knockBack * .15f,
                source.owner, (int)kind);
            p.CritChance = source.CritChance;
        }
    }
    public override void SetDefaults()
    {
        Projectile.width = 8; Projectile.height = 12;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 5; Projectile.timeLeft = 90;
        Projectile.ignoreWater = true; Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 15;
    }
    public override bool? CanDamage() => Projectile.ai[1] >= 5 ? null : false;
    public override void AI()
    {
        if (!VanguardSpearRules.ValidKind((int)Projectile.ai[0]) || !float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        { Projectile.Kill(); return; }
        Projectile.ai[1]++;
        if (Projectile.ai[1] > 5)
        {
            Projectile.velocity.X *= .985f;
            Projectile.velocity.Y = Math.Min(10, Projectile.velocity.Y + .2f);
        }
        VanguardSpearVfx.Spark(Projectile.Center, Projectile.velocity, (VanguardSpearKind)(int)Projectile.ai[0], (int)Projectile.ai[1]);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Two weak ricochets, then expire. Never persist inside terrain.
        if (++Projectile.ai[2] > 2) return true;
        if (oldVelocity.X != Projectile.velocity.X) Projectile.velocity.X = -oldVelocity.X * .4f;
        if (oldVelocity.Y != Projectile.velocity.Y) Projectile.velocity.Y = -oldVelocity.Y * .3f;
        Projectile.netUpdate = true;
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => VanguardSpearVfx.Spark(Projectile.Center, -Projectile.velocity * .3f, (VanguardSpearKind)(int)Projectile.ai[0], 0);
    public override bool PreDraw(ref Color lightColor) => false;
}
