using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ArknightsMod.Common.GlobalItems
{
    // Transplanted from CalamityLegendsComeBack: one shared crystal style for every profession and branch.
    internal sealed class OperatorProfessionTooltip : GlobalItem
    {
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            bool isProfession = line.Name == "OperatorClass";
            bool isBranch = line.Name == "OperatorBranch";
            if (line.Mod != Mod.Name || (!isProfession && !isBranch))
                return true;

            DrawCrystalWarning(line);
            return false;
        }

        private static void DrawCrystalWarning(DrawableTooltipLine line)
        {
            Vector2 position = new(line.X, line.Y);
            // HJSON multiline descriptions already contain real newlines. Each row
            // needs its own glyph origin and crystal frame; never draw '\n' as a glyph.
            foreach (string row in line.Text.Replace("\r", "").Split('\n'))
            {
                if (row.Length > 0)
                    DrawCrystalRow(line, row, position);
                position.Y += line.Font.LineSpacing * line.BaseScale.Y;
            }
        }

        private static void DrawCrystalRow(DrawableTooltipLine line, string text, Vector2 position)
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 size = line.Font.MeasureString(text) * line.BaseScale;
            Rectangle frame = new(
                (int)position.X - 5,
                (int)position.Y - 2,
                Math.Max(12, (int)Math.Ceiling(size.X) + 10),
                Math.Max(12, (int)Math.Ceiling(size.Y) + 4));

            // A dark glass backing keeps the warning readable over any tooltip background.
            DrawRect(frame, new Color(12, 16, 39, 225));
            DrawRect(new Rectangle(frame.X + 1, frame.Y + 1, frame.Width - 2, 1),
                Color.Lerp(new Color(77, 249, 255), new Color(235, 121, 255),
                    0.5f + 0.5f * MathF.Sin(time * 2.4f)) * 0.9f);
            DrawRect(new Rectangle(frame.X + 1, frame.Bottom - 2, frame.Width - 2, 1),
                new Color(101, 161, 255, 160));

            // A narrow moving reflection and faceted sparks give this line its own crystal look.
            float sweep = (time * 54f) % (frame.Width + 24f) - 12f;
            int sweepX = frame.X + (int)sweep;
            for (int step = -2; step <= 2; step++)
            {
                int x = sweepX + step;
                if (x > frame.X + 1 && x < frame.Right - 1)
                    DrawRect(new Rectangle(x, frame.Y + 2, 1, frame.Height - 4),
                        new Color(137, 238, 255, 0) * (0.13f - Math.Abs(step) * 0.022f));
            }

            for (int spark = 0; spark < 5; spark++)
            {
                float phase = time * (2.2f + spark * 0.17f) + spark * 1.79f;
                float brightness = MathF.Pow(Math.Max(0f, MathF.Sin(phase)), 4f);
                float x = frame.X + 12f + (frame.Width - 24f) * (spark + 0.5f) / 5f;
                float y = spark % 2 == 0 ? frame.Y + 2f : frame.Bottom - 2f;
                DrawFacet(new Vector2(x, y), 1.3f + brightness * 1.6f,
                    new Color(225, 252, 255, 0) * (0.25f + brightness * 0.55f));
            }

            // Individual glyphs shimmer at different phases; the warning remains legible at all times.
            float advance = 0f;
            for (int index = 0; index < text.Length; index++)
            {
                string glyph = text[index].ToString();
                Vector2 glyphPosition = position + Vector2.UnitX * advance;
                float wave = 0.5f + 0.5f * MathF.Sin(time * 3.8f - index * 0.52f);
                Color glow = Color.Lerp(new Color(38, 229, 255, 0), new Color(206, 105, 255, 0), wave) * 0.32f;

                DrawText(line, glyph, glyphPosition + new Vector2(-1.5f, 0f), glow);
                DrawText(line, glyph, glyphPosition + new Vector2(1.5f, 0f), glow);
                DrawText(line, glyph, glyphPosition + new Vector2(0f, -1.5f), glow);
                DrawText(line, glyph, glyphPosition + new Vector2(0f, 1.5f), glow);
                DrawText(line, glyph, glyphPosition + new Vector2(1f, 1f), new Color(2, 5, 20, 225));

                Color face = Color.Lerp(new Color(114, 234, 255), new Color(248, 189, 255), wave);
                if (Math.Abs(sweepX - glyphPosition.X - line.Font.MeasureString(glyph).X * line.BaseScale.X * 0.5f) < 10f)
                    face = Color.Lerp(face, Color.White, 0.55f);
                DrawText(line, glyph, glyphPosition, face);
                advance += line.Font.MeasureString(glyph).X * line.BaseScale.X;
            }
        }

        private static void DrawFacet(Vector2 position, float radius, Color color)
        {
            // Scale a single texel; the full MagicPixel texture is a tall strip.
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1), color,
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(radius * 2f),
                SpriteEffects.None, 0f);
        }

        private static void DrawRect(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }

        private static void DrawText(DrawableTooltipLine line, string text, Vector2 position, Color color)
        {
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, position, color,
                line.Rotation, line.Origin, line.BaseScale);
        }
    }
}
