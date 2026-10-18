using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace ArknightsMod.Common.UI.HeadBars
{
    /// <summary>
    /// A compact, stepped status bar for world-space player HUDs.
    /// Every emitted rectangle is clipped against <see cref="OuterWidth"/> x
    /// <see cref="OuterHeight"/>, so decoration can never extend beyond the bar.
    /// </summary>
    public static class BoundedHeadBarRenderer
    {
        public const int OuterWidth = 64;
        public const int OuterHeight = 14;
        private const int SegmentCount = 7;

        public static void AddToPlayerDrawCache(
            List<DrawData> drawDataCache,
            Vector2 center,
            float progress,
            Color backgroundColor,
            Color startColor,
            Color endColor,
            float opacity,
            float flash,
            float animationTime)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            DrawCore(
                drawDataCache,
                null,
                pixel,
                center,
                progress,
                backgroundColor,
                startColor,
                endColor,
                opacity,
                flash,
                animationTime);
        }

        public static void DrawImmediate(
            SpriteBatch spriteBatch,
            Vector2 center,
            float progress,
            Color backgroundColor,
            Color startColor,
            Color endColor,
            float opacity,
            float flash,
            float animationTime)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            DrawCore(
                null,
                spriteBatch,
                pixel,
                center,
                progress,
                backgroundColor,
                startColor,
                endColor,
                opacity,
                flash,
                animationTime);
        }

        private static void DrawCore(
            List<DrawData> drawDataCache,
            SpriteBatch spriteBatch,
            Texture2D pixel,
            Vector2 center,
            float progress,
            Color backgroundColor,
            Color startColor,
            Color endColor,
            float opacity,
            float flash,
            float animationTime)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            opacity = MathHelper.Clamp(opacity, 0f, 1f);
            flash = MathHelper.Clamp(flash, 0f, 1f);

            Rectangle bounds = new(
                (int)MathF.Round(center.X) - OuterWidth / 2,
                (int)MathF.Round(center.Y) - OuterHeight / 2,
                OuterWidth,
                OuterHeight);

            Color background = backgroundColor * opacity;
            Color frame = Color.Lerp(startColor, endColor, 0.35f) * opacity;
            Color brightFrame = Color.Lerp(frame, Color.White, flash * 0.72f);

            // Five non-overlapping rows form stepped/chamfered ends without diagonal
            // line primitives or protruding corner brackets.
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X + 4, bounds.Y, bounds.Width - 8, 2), background);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, 2), background);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X, bounds.Y + 4, bounds.Width, bounds.Height - 8), background);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X + 2, bounds.Bottom - 4, bounds.Width - 4, 2), background);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X + 4, bounds.Bottom - 2, bounds.Width - 8, 2), background);

            // Short inset highlights keep the silhouette readable without recreating
            // four L-shaped corners.
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X + 8, bounds.Y + 1, bounds.Width - 16, 1), brightFrame * 0.82f);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X + 4, bounds.Y + 3, 6, 1), frame * 0.72f);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.Right - 10, bounds.Y + 3, 6, 1), frame * 0.72f);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, new Rectangle(bounds.X + 8, bounds.Bottom - 2, bounds.Width - 16, 1), frame * 0.38f);

            Rectangle fillBounds = new(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10);
            EmitClipped(drawDataCache, spriteBatch, pixel, bounds, fillBounds, Color.Lerp(backgroundColor, Color.Black, 0.52f) * (opacity * 0.9f));

            int filledRight = fillBounds.X + (int)MathF.Round(fillBounds.Width * progress);
            float segmentSpan = (fillBounds.Width + SegmentCount - 1f) / SegmentCount;
            for (int i = 0; i < SegmentCount; i++)
            {
                int segmentLeft = fillBounds.X + (int)MathF.Floor(i * segmentSpan);
                int segmentRight = fillBounds.X + (int)MathF.Floor((i + 1) * segmentSpan) - 1;
                segmentRight = Math.Min(segmentRight, fillBounds.Right);
                int activeRight = Math.Min(segmentRight, filledRight);
                if (activeRight <= segmentLeft)
                    continue;

                float colorInterpolant = SegmentCount <= 1 ? 0f : i / (float)(SegmentCount - 1);
                Color segmentColor = Color.Lerp(startColor, endColor, colorInterpolant);
                segmentColor = Color.Lerp(segmentColor, Color.White, flash * 0.68f) * opacity;
                EmitClipped(
                    drawDataCache,
                    spriteBatch,
                    pixel,
                    bounds,
                    new Rectangle(segmentLeft, fillBounds.Y, activeRight - segmentLeft, fillBounds.Height),
                    segmentColor);
            }

            // A one-pixel scan glint is allowed only inside the already-filled area.
            if (filledRight > fillBounds.X)
            {
                float sweep = animationTime * 0.72f % 1f;
                int sweepX = fillBounds.X + (int)(fillBounds.Width * sweep);
                if (sweepX < filledRight)
                {
                    EmitClipped(
                        drawDataCache,
                        spriteBatch,
                        pixel,
                        bounds,
                        new Rectangle(sweepX, fillBounds.Y, 1, fillBounds.Height),
                        Color.White * (opacity * 0.58f));
                }
            }
        }

        private static void EmitClipped(
            List<DrawData> drawDataCache,
            SpriteBatch spriteBatch,
            Texture2D pixel,
            Rectangle bounds,
            Rectangle rectangle,
            Color color)
        {
            Rectangle clipped = Rectangle.Intersect(bounds, rectangle);
            if (clipped.Width <= 0 || clipped.Height <= 0 || color == Color.Transparent)
                return;

            if (drawDataCache != null)
                drawDataCache.Add(new DrawData(pixel, new Vector2(clipped.X, clipped.Y), new Rectangle(0, 0, 1, 1),
                    color, 0, Vector2.Zero, new Vector2(clipped.Width, clipped.Height), SpriteEffects.None));
            else
                spriteBatch.Draw(pixel, clipped, color);
        }
    }
}
