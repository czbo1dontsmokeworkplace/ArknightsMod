using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

// Networked, visual-only feedback for the opening contact, never a delayed damage pulse.
public sealed class DreadnoughtHeavyImpact : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 24;
        Projectile.penetrate = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Projectile.localAI[0]++ != 0 || !ChargedOperatorVfx.Visible(Projectile.Center)) return;
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .8f, Pitch = -.35f, MaxInstances = 4 }, Projectile.Center);
        SoundEngine.PlaySound(SoundID.Item71 with { Volume = .9f, Pitch = -.5f, MaxInstances = 4 }, Projectile.Center);
        Color color = ChargedOperatorRules.ColorFor(Kind);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        for (int i = 0; i < 26 && ParticleManager.activeParticles.Count < 1200; i++)
        {
            Vector2 velocity = aim.RotatedByRandom(.8f) * Main.rand.NextFloat(4, 19);
            if (i % 4 == 0) velocity *= -0.5f;
            new T_Smear_Particle(Projectile.Center + Main.rand.NextVector2Circular(10, 10), velocity,
                Main.rand.Next(14, 29), Color.Lerp(color, Color.White, Main.rand.NextFloat(.15f, .6f)),
                Main.rand.NextFloat(24, 65), Main.rand.NextFloat(5, 13), .88f, 1.6f).Spawn();
            if (i % 2 == 0) ChargedOperatorVfx.Sparkle(Projectile.Center, velocity * .6f, color, velocity.ToRotation(), 1.5f);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float t = 1 - Projectile.timeLeft / 24f;
        float fade = MathF.Pow(1 - t, 2);
        Color color = ChargedOperatorRules.ColorFor(Kind);
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite("RuptureRing", Projectile.Center, new Vector2(45 + 240 * t, 25 + 100 * t), color * fade * .9f,
            Projectile.velocity.ToRotation());
        // LinearSpark's long axis is vertical in source pixels. Align that axis
        // with the blade tangent; do not add the sword sprite's diagonal correction.
        ChargedOperatorVfx.Sprite("LinearSpark", Projectile.Center, new Vector2(55, 170) * (1 + t), Color.Lerp(color, Color.White, .7f) * fade,
            Projectile.velocity.ToRotation() - MathHelper.PiOver2);
        ChargedOperatorVfx.Sprite("StarCore", Projectile.Center, new Vector2(64) * (1 + t), color * fade * .8f);
        return false;
    }
}
