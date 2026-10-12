using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

internal static class CrossbowFlightVfx
{
    internal static void Draw(Projectile p, CrossbowKind kind, Vector2[] history = null, bool drawArrow = true)
    {
        Color color = CrossbowVisuals.ColorFor(kind), white = Color.Lerp(color, Color.White, .72f);
        int tier = (int)kind;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        float angle = aim.ToRotation();
        OperatorFlightVfx.Trail(p.Center, history ?? p.oldPos, history == null ? p.Size * .5f : Vector2.Zero,
            color, white, 5f + tier * 2.2f, 100f + tier * 52f);
        if (tier >= 1)
        {
            // A split, open wake makes advanced bolts sharper, without widening their hit silhouette.
            OperatorFlightVfx.Sprite("ForwardFlame", p.Center - aim * (18f + tier * 4f),
                new Vector2(12f + tier * 3f, 52f + tier * 15f), OperatorFlightVfx.Add(color, .35f), angle + MathHelper.PiOver2);
        }
        if (tier >= 2)
        {
            Vector2 side = aim.RotatedBy(MathHelper.PiOver2);
            float phase = Main.GlobalTimeWrappedHourly * 18f + p.identity;
            for (int i = 0; i < 8; i++)
            {
                float fade = 1f - i / 8f;
                float wave = MathF.Sin(phase - i * .75f) * (7f + tier);
                for (int sign = -1; sign <= 1; sign += 2)
                    OperatorFlightVfx.Sprite("LinearSpark", p.Center - aim * (8f + i * 9f) + side * wave * sign,
                        new Vector2(1.8f, 12f), OperatorFlightVfx.Add(sign > 0 ? white : color, fade * .75f), angle + MathHelper.PiOver2);
            }
        }
        if (drawArrow)
        {
            Texture2D arrow = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Sniper/KroosAlter/KroosAlterCrossbow_Arrow").Value;
            Main.EntitySpriteDraw(arrow, p.Center - Main.screenPosition, null,
                kind == CrossbowKind.Schwarz ? new Color(21, 14, 32) : white,
                angle + MathHelper.PiOver2, arrow.Size() * .5f, 1f, SpriteEffects.None);
        }
        OperatorFlightVfx.Sprite("LinearSpark", p.Center + aim * 5f, new Vector2(3f, 21f),
            OperatorFlightVfx.Add(white, .8f), angle + MathHelper.PiOver2);
    }

    internal static void Wake(Projectile p, CrossbowKind kind)
    {
        if (Main.dedServ || p.numUpdates != 0) return;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX), side = aim.RotatedBy(MathHelper.PiOver2);
        int tier = (int)kind;
        for (int i = 0; i < (tier >= 2 ? 2 : 1); i++)
        {
            float phase = Main.GlobalTimeWrappedHourly * 18f + p.identity;
            float offset = tier >= 2 ? MathF.Sin(phase + i * MathHelper.Pi) * (tier + 7f) : Main.rand.NextFloat(-3f, 3f);
            OperatorFlightVfx.Spark(p.Center - aim * 8f + side * offset, -aim * Main.rand.NextFloat(1.5f, 3.5f),
                CrossbowVisuals.ColorFor(kind), 12f + tier * 5f, 12 + tier * 3, 1.5f + tier * .3f);
        }
    }
}
