using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain;

// Slagfire Douser's invisible, spark-led flame rebuilt with this mod's particles.
public sealed class LaevatainSlagfire : ModProjectile
{
    private static readonly Color Outer = new(142, 12, 30);
    private static readonly Color Core = new(235, 67, 63);

    public override string Texture => "ArknightsMod/Content/Dusts/Fire/fire_18";

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 180;
        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 10;
    }

    public override void AI()
    {
        Projectile.localAI[1]++;
        if (Projectile.localAI[1] < 4f)
            Projectile.tileCollide = false;

        Projectile.scale -= 0.001f;
        if (Projectile.scale <= 0f) {
            Projectile.Kill();
            return;
        }

        if (++Projectile.localAI[0] > 3f && !Main.dedServ && ParticleManager.activeParticles.Count < 1400) {
            for (int i = 0; i < 3; i++) {
                Vector2 position = Projectile.Center - Projectile.velocity * (i / 3f);
                Vector2 velocity = -Projectile.velocity * Main.rand.NextFloat(0.005f, 0.015f);
                new LaevatainSlagSpark(position, velocity, Main.rand.NextBool(4) ? Core : Outer,
                    Main.rand.NextFloat(8f, 13f) * Projectile.scale, 6).Spawn();
            }
            if (Main.rand.NextBool(8))
                new LaevatainSlagSpark(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f),
                    -Projectile.velocity * Main.rand.NextFloat(0.01f, 0.045f), Outer,
                    Main.rand.NextFloat(6f, 10f), 8).Spawn();
            if (Main.rand.NextBool(50))
                new LaevatainSlagDrop(Projectile.Bottom + new Vector2(0f, 3f),
                    new Vector2(Main.rand.NextFloat(-1f, 1f), 3f), Main.rand.Next(30, 50),
                    Main.rand.NextFloat(0.45f, 0.7f), Outer).Spawn();
        }

        if (Projectile.localAI[1] >= 10f)
            Projectile.velocity.Y += 0.2f;
        Lighting.AddLight(Projectile.Center, 0.32f * Projectile.scale, 0.035f * Projectile.scale, 0.045f * Projectile.scale);
    }

    public override bool PreDraw(ref Color lightColor) => false;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Burst(Projectile.Center, Vector2.UnitX * Projectile.direction);
        return true;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, 300);
        Vector2 position = target.Center + new Vector2(
            (target.width * 0.5f + 4f) * Projectile.direction,
            Main.rand.NextFloat(-target.height * 0.5f, target.height * 0.5f));
        Burst(position, Vector2.UnitX * Projectile.direction);
    }

    private static void Burst(Vector2 position, Vector2 direction)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1400)
            return;

        for (int i = 0; i < 4; i++) {
            Vector2 velocity = direction.RotatedBy(Main.rand.NextFloat(-MathHelper.ToRadians(50f), MathHelper.ToRadians(50f)))
                * Main.rand.NextFloat(10f, 13f);
            int lifetime = Main.rand.Next(20, 45);
            float scale = Main.rand.NextFloat(0.3f, 0.7f);
            new LaevatainSlagDrop(position + Main.rand.NextVector2Circular(1f, 1f),
                velocity, lifetime, scale * 1.15f, Outer).Spawn();
            new LaevatainSlagDrop(position + Main.rand.NextVector2Circular(1f, 1f),
                velocity, lifetime, scale * 0.75f, Core).Spawn();
        }
    }
}

public sealed class LaevatainSlagSpark : Particle
{
    public override string TexturePath => "ArknightsMod/Content/Projectiles/Guard/Mousse/Assets/MousseLightSplinter";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly float length;

    public LaevatainSlagSpark(Vector2 position, Vector2 velocity, Color color, float length, int lifetime)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        this.length = length;
        Lifetime = lifetime;
    }

    public override void Update() => Velocity *= 0.88f;

    public override void Draw()
    {
        float fade = 1f - LifetimeRatio;
        Vector2 scale = new Vector2(length, length * 0.4f) / Texture.Size();
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * fade,
            Velocity.LengthSquared() > 0.01f ? Velocity.ToRotation() : 0f,
            Texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }
}

public sealed class LaevatainSlagDrop : Particle
{
    public override string TexturePath => "ArknightsMod/Content/ElementalImpairment/Effect/FireParticle";
    public override BlendState DrawBlendState => BlendState.Additive;

    public LaevatainSlagDrop(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        Scale = scale;
        Color = color;
    }

    public override void Update()
    {
        Velocity.X *= 0.97f;
        Velocity.Y += 0.25f;
        Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
    }

    public override void Draw()
    {
        float fade = MathF.Pow(1f - LifetimeRatio, 1.3f);
        Vector2 size = new Vector2(8f, 12f) * Scale * fade;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * fade,
            Rotation, Texture.Size() * 0.5f, size / Texture.Size(), SpriteEffects.None, 0f);
    }
}
