using System;
using ArknightsMod.Content.Projectiles.Developer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ArknightsMod.Content.Items.Developer;

// MurasamaBloodLegendaryTooltip's per-glyph shimmer, re-themed as a phosphor sweep.
// The background is a finished sprite, not a stretched MagicPixel strip.
internal static class SR25Tooltip
{
    internal static void Draw(DrawableTooltipLine line)
    {
        float time = Main.GlobalTimeWrappedHourly;
        Vector2 size = line.Font.MeasureString(line.Text) * line.BaseScale;
        Texture2D panel = SR25Visuals.Art("TooltipPlate");
        Main.spriteBatch.Draw(panel, new Rectangle(line.X - 7, line.Y - 2, (int)MathF.Ceiling(size.X) + 14,
            (int)MathF.Ceiling(line.Font.LineSpacing * line.BaseScale.Y) + 4), Color.White);
        float sweep = time * 65 % (size.X + 64) - 32;
        float x = line.X;
        for (int i = 0; i < line.Text.Length; i++)
        {
            string glyph = line.Text[i].ToString();
            float advance = line.Font.MeasureString(glyph).X * line.BaseScale.X;
            float glint = MathHelper.Clamp(1 - Math.Abs(sweep - (x - line.X + advance * .5f)) / 30, 0, 1);
            float pulse = .5f + .5f * MathF.Sin(time * 2.8f - i * .32f);
            Color face = Color.Lerp(new Color(78, 196, 128), new Color(190, 255, 210), pulse);
            face = Color.Lerp(face, Color.White, glint * .85f);
            Vector2 position = new(x, line.Y + MathF.Sin(time * 2 + i * .3f) * .25f);
            Color glow = new Color(60, 255, 144, 0) * (.15f + glint * .35f);
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, glyph, position - Vector2.UnitX,
                glow, line.Rotation, line.Origin, line.BaseScale);
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, glyph, position + Vector2.UnitX,
                glow, line.Rotation, line.Origin, line.BaseScale);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, line.Font, glyph, position, face,
                line.Rotation, line.Origin, line.BaseScale);
            x += advance;
        }
    }
}
