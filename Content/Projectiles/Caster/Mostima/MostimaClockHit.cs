using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// A synchronized, non-damaging event at the true hit location. Only local visual
// systems own the emitted particles; none follows the clock or NPC afterwards.
public sealed class MostimaClockHit : ModProjectile
{
    public override string Texture => "ArknightsMod/Assets/null";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = 24;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 center = Projectile.Center;
        if (!SpreadVfxBudget.Visible(center, 140f)) return false;
        float age = 24f - Projectile.timeLeft;
        float t = MathHelper.Clamp(age / 24f, 0f, 1f);
        float size = MathHelper.Clamp(Projectile.ai[0], 16f, 64f);
        float gold = MathHelper.Clamp(Projectile.ai[1], 0f, 1f);
        Color blue = MostimaClockwork.TimeColor(new Color(32, 117, 250), gold);
        Color cyan = MostimaClockwork.TimeColor(new Color(72, 213, 255), gold);
        Color core = MostimaClockwork.TimeColor(new Color(211, 249, 255), gold);
        float radius = (size + 32f) * (.4f + .6f * (1f - MathF.Pow(1f - t, 3f)));
        float ringFade = MathHelper.Clamp(age / 2f, 0f, 1f) * MathF.Pow(1f - t, .8f);
        float flash = MathF.Pow(MathF.Max(0f, 1f - age / 10f), 2f);
        using (SpreadArtsMaterial.Begin())
        {
            // Original-game composition: blue aura, compact white-blue impact,
            // one fine cyan perimeter. No opaque disk or petal-shaped explosion.
            SpreadArtsMaterial.Sprite("ChargeBloom", center, new Vector2(radius * 1.65f),
                OperatorFlightVfx.Add(blue, .55f * (1f - t)));
            SpreadArtsMaterial.Sprite("ChargeBloom", center, new Vector2(size * 1.15f, size * 1.45f),
                OperatorFlightVfx.Add(cyan, flash));
            SpreadArtsMaterial.Sprite("ChargeBloom", center, new Vector2(size * .6f, size * .9f),
                OperatorFlightVfx.Add(core, flash));
        }
        const int segments = 96;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * MathHelper.TwoPi / segments;
            Vector2 a = center + angle.ToRotationVector2() * radius;
            Vector2 b = center + (angle + MathHelper.TwoPi / segments).ToRotationVector2() * radius;
            MostimaClockwork.Line(a, b, OperatorFlightVfx.Add(cyan, ringFade * .8f), 1.4f);
        }
        return false;
    }
    public override void AI()
    {
        if (Main.dedServ || Projectile.localAI[0] != 0f) return;
        Projectile.localAI[0] = 1f;
        MostimaGrandClockVfx.Hit(Projectile.Center, Projectile.ai[0], Projectile.ai[1]);
    }
}
