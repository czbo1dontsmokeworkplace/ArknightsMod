using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Common.VisualEffects;

/// <summary>单发蓄力武器共用的头顶进度条与一次性满蓄力提示。</summary>
internal static class ChargeCastFeedback
{
    internal static void DrawBar(Player owner, float progress, Color color)
    {
        if (Main.dedServ || !owner.active) return;
        progress = MathHelper.Clamp(progress, 0f, 1f);
        Vector2 center = owner.Top - new Vector2(0f, 26f) - Main.screenPosition;
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Rectangle frame = new((int)center.X - 37, (int)center.Y - 5, 74, 10);
        Rectangle inner = new(frame.X + 2, frame.Y + 2, 70, 6);
        Main.spriteBatch.Draw(pixel, frame, new Color(9, 10, 22) * .88f);
        Main.spriteBatch.Draw(pixel, inner, new Color(54, 57, 68) * .72f);
        if (progress > 0f)
        {
            int width = Math.Max(1, (int)MathF.Round(inner.Width * progress));
            Main.spriteBatch.Draw(pixel, new Rectangle(inner.X, inner.Y, width, inner.Height),
                Color.Lerp(color * .62f, color, progress));
            Main.spriteBatch.Draw(pixel, new Rectangle(inner.X + width - 1, inner.Y, 1, inner.Height),
                Color.White * (progress >= 1f ? .95f : .58f));
        }
        if (progress >= 1f)
        {
            float pulse = .65f + .35f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f);
            Main.spriteBatch.Draw(pixel, new Rectangle(frame.X, frame.Y - 1, frame.Width, 1), color * pulse);
        }
    }

    internal static void Ready(Vector2 position, Color color, int dustType, float pitch)
    {
        if (Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Item29 with
        { Volume = .63f, Pitch = pitch, MaxInstances = 4 }, position);
        if (ParticleManager.activeParticles.Count < 1300)
            new T_Ready_Ripple_Particle(position, color, 20, 88f).Spawn();
        for (int i = 0; i < 14; i++)
        {
            Vector2 direction = (MathHelper.TwoPi * i / 14f + Main.rand.NextFloat(-.09f, .09f))
                .ToRotationVector2();
            Dust dust = Dust.NewDustPerfect(position + direction * Main.rand.NextFloat(7f, 15f),
                dustType, direction * Main.rand.NextFloat(2.5f, 5.5f), 90, color,
                Main.rand.NextFloat(.62f, .94f));
            dust.noGravity = true;
        }
    }
}
