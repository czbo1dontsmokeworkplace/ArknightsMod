using ArknightsMod.Content.Buffs;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaCrossBurst : ModProjectile
{
    public override string Texture => MostimaVisuals.Assets + "ChronoCore";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 560;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 65;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }
    // Six possible hits at ten-tick intervals; the clock remains visible for all 65 ticks.
    public override bool? CanDamage() => Projectile.timeLeft <= 55 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 p = targetHitbox.ClosestPointInRect(Projectile.Center);
        // The clock face is the blast region; cross pointers mark the same bounded diameter.
        return Vector2.DistanceSquared(p, Projectile.Center) <= 280 * 280;
    }
    public override void AI()
    {
        if (Main.dedServ) return;
        if (Projectile.timeLeft == 65)
        {
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = .7f, Pitch = -.48f }, Projectile.Center);
            // The fixed face and indices are drawn by MostimaVisuals.Burst.
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = (i * MathHelper.TwoPi / 12).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dir * 220f, DustID.BlueCrystalShard,
                    -dir * Main.rand.NextFloat(.5f, 1.5f), 110, MostimaVisuals.Blue, .65f);
                dust.noGravity = true;
            }
        }
        if (Projectile.timeLeft == 55)
        {
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = .86f, Pitch = -.28f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .45f, Pitch = .24f }, Projectile.Center);
            if (Vector2.DistanceSquared(Main.LocalPlayer.Center, Projectile.Center) < 1000f * 1000f)
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center,
                    Vector2.UnitY, 5f, 4f, 14));
            MostimaVisuals.Particles(Projectile.Center, 18, 8f);
            OperatorFlightVfx.Ring(Projectile.Center, MostimaVisuals.Blue, 250f, 20);
            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = (i * MathHelper.TwoPi / 16f).ToRotationVector2();
                Vector2 velocity = dir * Main.rand.NextFloat(3f, 6f);
                OperatorFlightVfx.Spark(Projectile.Center + dir * 28f, velocity,
                    i % 4 == 0 ? MostimaVisuals.Violet : MostimaVisuals.Blue,
                    Main.rand.NextFloat(12f, 24f), Main.rand.Next(11, 20), 2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dir * 36f,
                    DustID.BlueCrystalShard, velocity * .76f, 100,
                    i % 5 == 0 ? Color.White : MostimaVisuals.Blue,
                    Main.rand.NextFloat(.55f, .85f));
                dust.noGravity = true;
            }
        }
        if (Projectile.timeLeft == 39)
        {
            MostimaVisuals.Particles(Projectile.Center, 10, 5f);
            OperatorFlightVfx.Ring(Projectile.Center, MostimaVisuals.Violet, 240f, 18);
        }
        if (Projectile.timeLeft <= 55 && Projectile.timeLeft % 10 == 5
            && ParticleManager.activeParticles.Count < 1500)
            for (int i = 0; i < 24; i++)
            {
                float angle = i * MathHelper.TwoPi / 24f;
                Vector2 ray = angle.ToRotationVector2();
                new MostimaTemporalShard(Projectile.Center + ray * 210f,
                    ray * Main.rand.NextFloat(1.8f, 4f), MostimaVisuals.Blue, 22, 18f).Spawn();
                if (i % 2 == 0)
                    new MostimaOrbitGlyph(Projectile.Center, 265f, angle, i % 4 == 0 ? .012f : -.012f,
                        MostimaVisuals.Violet, 24).Spawn();
            }
        Lighting.AddLight(Projectile.Center, .1f, .3f, .5f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!target.boss && target.realLife < 0) OperatorStunNPC.TryApply(target, 20);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MostimaVisuals.Burst(Projectile.Center, 65 - Projectile.timeLeft);
        return false;
    }
}
