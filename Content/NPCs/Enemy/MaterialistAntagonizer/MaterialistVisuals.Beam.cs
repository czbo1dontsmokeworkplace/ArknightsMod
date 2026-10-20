using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawNativeBeam(MaterialistHazard h, Color color)
    {
        Vector2 origin = h.Projectile.Center, direction = h.BeamDirection;
        float reach = h.Reach;
        Brackets(origin, new Vector2(18), color, .7f);
        if (h.Age < h.Delay)
        {
            if (h.Sweep != 0)
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 axis = h.Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(side * h.Sweep * .5f);
                    Line(origin, origin + axis * MaterialistHazard.TraceBeam(origin, axis, h.Width), color * .20f, 1);
                }
            Line(origin, origin + direction * reach, color * .65f, 2);
            Glow(origin, color * .7f, new Vector2(20 + h.Age * .5f));
            return;
        }
        float strength = h.BeamStrength;
        if (reach <= .5f || strength <= 0) return;
        Main.instance.LoadProjectile(ProjectileID.PhantasmalDeathray);
        Texture2D start = TextureAssets.Projectile[ProjectileID.PhantasmalDeathray].Value;
        Texture2D middle = TextureAssets.Extra[21].Value;
        Texture2D end = TextureAssets.Extra[22].Value;
        // The same start/body/end geometry is used for each layer. No long solid rectangle.
        MaterialistTechRenderer.NativeLaser(() =>
        {
            if (!Reduced)
                DrawBeamLayer(h, start, middle, end, h.Width * strength * 1.85f,
                    new Color(125, 42, 235) * (.23f * strength * layerOpacity));
            DrawBeamLayer(h, start, middle, end, h.Width * strength * 1.15f,
                color * (.88f * strength * layerOpacity));
            DrawBeamLayer(h, start, middle, end, h.Width * strength * .46f,
                new Color(244, 217, 255) * (.92f * strength * layerOpacity));
        });
        Glow(origin, new Color(235, 207, 255) * (.8f * strength), new Vector2(42));
        Glow(origin + direction * reach, color * (.65f * strength), new Vector2(30));
    }

    private static void DrawBeamLayer(MaterialistHazard h, Texture2D start, Texture2D middle, Texture2D end,
        float width, Color tint)
    {
        Vector2 origin = h.Projectile.Center, direction = h.BeamDirection;
        float angle = direction.ToRotation() - MathHelper.PiOver2;
        float length = h.Reach;
        float scaleY = Math.Max(.5f, h.Width / (float)middle.Width);
        float startLength = Math.Min(start.Height * scaleY, length * .35f);
        float endLength = Math.Min(end.Height * scaleY, length * .35f);
        float bodyEnd = length - endLength;
        DrawBeamPiece(start, start.Bounds, origin, angle, width, startLength, tint);
        // Only draw body segments near the viewport even if an open sky ray crosses the world.
        float visibleStart = 0, visibleEnd = length;
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Vector2 padding = new Vector2(200) + new Vector2(Main.screenWidth * Math.Max(0, 1 / Math.Max(.25f, zoom.X) - 1),
            Main.screenHeight * Math.Max(0, 1 / Math.Max(.25f, zoom.Y) - 1)) * .5f;
        Vector2 viewMin = Main.screenPosition - padding;
        Vector2 viewMax = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) + padding;
        if (ClipBeamView(origin.X, direction.X, viewMin.X, viewMax.X, ref visibleStart, ref visibleEnd) &&
            ClipBeamView(origin.Y, direction.Y, viewMin.Y, viewMax.Y, ref visibleStart, ref visibleEnd))
        {
            float segmentLength = 16 * scaleY;
            int first = Math.Max(0, (int)MathF.Floor((visibleStart - startLength) / segmentLength));
            int frames = Math.Max(1, middle.Height / 16);
            for (int i = first; ; i++)
            {
                float along = startLength + i * segmentLength;
                if (along >= bodyEnd || along > visibleEnd) break;
                float partLength = Math.Min(segmentLength, bodyEnd - along);
                Rectangle frame = new(0, ((h.Age / 3 + i) % frames) * 16, middle.Width,
                    Math.Max(1, (int)MathF.Ceiling(partLength / scaleY)));
                DrawBeamPiece(middle, frame, origin + direction * along, angle, width, partLength, tint);
            }
        }
        DrawBeamPiece(end, end.Bounds, origin + direction * bodyEnd, angle, width, endLength, tint);
    }

    private static void DrawBeamPiece(Texture2D texture, Rectangle frame, Vector2 position,
        float angle, float width, float length, Color tint)
    {
        if (length <= 0) return;
        MaterialistTechRenderer.NativeLaserTexture(texture);
        Main.spriteBatch.Draw(texture, position - Main.screenPosition, frame, tint with { A = 0 }, angle,
            new Vector2(frame.Width * .5f, 0), new Vector2(width / frame.Width, length / frame.Height), SpriteEffects.None, 0);
    }

    private static bool ClipBeamView(float origin, float direction, float min, float max, ref float enter, ref float exit)
    {
        if (Math.Abs(direction) < .000001f) return origin >= min && origin <= max;
        float a = (min - origin) / direction, b = (max - origin) / direction;
        enter = Math.Max(enter, Math.Min(a, b));
        exit = Math.Min(exit, Math.Max(a, b));
        return enter <= exit;
    }
}
