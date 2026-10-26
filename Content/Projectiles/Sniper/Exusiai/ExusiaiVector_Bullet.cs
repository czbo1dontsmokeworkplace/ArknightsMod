using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

public class ExusiaiVector_Bullet : ModProjectile
{
    private Vector2 launch;
    private bool initialized;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.timeLeft = 240;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 6; Projectile.penetrate = 1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        if (!initialized) { initialized = true; launch = Projectile.Center; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates == 0)
            Lighting.AddLight(Projectile.Center, VectorArt.Theme((int)Projectile.ai[0]).ToVector3() * .3f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        VectorArt.Round(Main.spriteBatch, Projectile.Center - Main.screenPosition, Projectile.velocity,
            initialized ? Vector2.Distance(launch, Projectile.Center) : 0, (int)Projectile.ai[0]);
        return false;
    }
    public override void OnKill(int timeLeft)
    {
        // One compact, networked visual burst; decoration never creates damaging projectiles.
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                -Projectile.velocity.SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<VectorImpact>(),
                0, 0, Projectile.owner, Projectile.ai[0], Projectile.identity % 127);
    }
}

public sealed class VectorImpact : ModProjectile
{
    private static ulong budgetFrame;
    private static int dustThisFrame;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 15;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (Main.dedServ || Projectile.localAI[0]++ != 0 ||
            Vector2.DistanceSquared(Projectile.Center, Main.LocalPlayer.Center) > 2200 * 2200) return;
        if (budgetFrame != Main.GameUpdateCount) { budgetFrame = Main.GameUpdateCount; dustThisFrame = 0; }
        for (int i = 0; i < 3 && dustThisFrame < 24; i++, dustThisFrame++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, i == 0 ? DustID.Smoke : DustID.Ash,
                Projectile.velocity.RotatedByRandom(.9f) * Main.rand.NextFloat(1, 4), 150,
                i == 0 ? new Color(116, 101, 91) : new Color(229, 185, 106), .6f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        VectorArt.Impact(Main.spriteBatch, Projectile.Center - Main.screenPosition, Projectile.velocity,
            1 - Projectile.timeLeft / 15f, (int)Projectile.ai[1], (int)Projectile.ai[0]);
        return false;
    }
}
