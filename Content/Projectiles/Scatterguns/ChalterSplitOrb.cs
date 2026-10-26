using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Scatterguns;

// CoreOfCalamitySplitOrb-inspired presentation over Ch'en's original shotgun flight.
// The reference's slowdown, target lock and jet acceleration are intentionally absent.
public sealed class ChalterSplitOrb : ModProjectile
{
    internal Color Tint => new(106, 218, 255);
    private int Mode => (int)Projectile.ai[1];
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 18;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = 150; Projectile.penetrate = 5;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source) => Projectile.rotation = Projectile.velocity.ToRotation();
    public override void AI()
    {
        Projectile.ai[0]++;
        Projectile.ai[2] += Projectile.velocity.Length();
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.velocity.Y += .025f;
        if (Projectile.ai[2] > 1230) { Projectile.Kill(); return; }
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, Tint.ToVector3() * .58f);
        if (Projectile.numUpdates != 0) return;
        ScatterVisuals.WaterBubbles(Projectile);
        Vector2 directionBack = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
        if (Projectile.ai[0] % 4 == 0)
        {
            float phase = Projectile.identity * 1.7f + Projectile.ai[0] * .09f;
            Vector2 side = directionBack.RotatedBy(MathHelper.PiOver2) * (4 + Math.Abs(MathF.Sin(phase)) * 5);
            for (int sign = -1; sign <= 1; sign += 2)
                OperatorFlightVfx.Spark(Projectile.Center + side * sign + directionBack * 15,
                    directionBack * Main.rand.NextFloat(.35f, 1.2f), Tint, 9, 10, 2);
        }
        OperatorFlightVfx.Spark(Projectile.Center - Projectile.velocity, -Projectile.velocity * .3f,
            Tint * .65f, 16, 21, 2);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        float range = MathHelper.Clamp((Projectile.ai[2] - 120) / 480, 0, 1);
        modifiers.SourceDamage *= MathHelper.Lerp(1.2f, .65f, range);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.Wet, 240);
        if (Mode > 0) target.AddBuff(ModContent.BuffType<HighPressureSoak>(), Mode == 2 ? 180 : 90);
        if (Mode == 2 && Projectile.owner == Main.myPlayer
            && Main.player[Projectile.owner].ownedProjectileCounts[ModContent.ProjectileType<ChalterWaterField>()] < 3)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<ChalterWaterField>(), Math.Max(1, Projectile.damage / 5), 0, Projectile.owner);
        if (!Main.dedServ)
            for (int i = 0; i < 5; i++)
                OperatorFlightVfx.Spark(target.Center, Main.rand.NextVector2Circular(4, 4), Tint, 16, 15, 2);
        Projectile.damage = Math.Max(1, (int)(Projectile.damage * .8f));
    }
    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ) return;
        OperatorFlightVfx.Ring(Projectile.Center, Tint, 70, 14);
        for (int i = 0; i < 8; i++)
            OperatorFlightVfx.Spark(Projectile.Center,
                (MathHelper.PiOver4 * i + Projectile.rotation).ToRotationVector2() * Main.rand.NextFloat(2.8f, 7.5f),
                Tint, 20, Main.rand.Next(10, 19), 3);
        ScatterVisuals.WaterBubbles(Projectile, 5);
    }
    public override bool PreDraw(ref Color lightColor) => false; // Shared shader pass.
    internal void DrawCore()
    {
        for (int i = 0; i < 3; i++)
            ShpcEnergyVisuals.Bloom(Projectile.Center, new Vector2(38, 27) * (1 - i * .2f),
                Color.Lerp(Tint, Color.White, i * .35f) * .76f, Projectile.rotation);
        float pulse = .8f + Math.Abs(MathF.Sin(Projectile.ai[0] * .15f / MathHelper.Pi)) * .2f;
        for (int i = 0; i < 10; i++)
            ShpcEnergyVisuals.Bloom(Projectile.Center + (MathHelper.TwoPi * i / 10).ToRotationVector2() * 4,
                new Vector2(38, 27), Tint * pulse * .06f, Projectile.rotation);
    }
}
