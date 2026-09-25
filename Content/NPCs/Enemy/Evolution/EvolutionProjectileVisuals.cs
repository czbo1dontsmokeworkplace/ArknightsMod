using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Presentation only: textured bodies share the existing hazard positions and collision clocks.
// Eight independent grayscale sprites: clean fantasy spell effects, never anatomical textures.
// Source bounds remove only transparent margins at draw time; scale is based on world size,
// not source resolution. Never change SpriteBatch state inside a projectile's PreDraw.
internal static class EvolutionProjectileVisuals
{
    internal readonly record struct Region(string Name, Rectangle Area);
    internal static readonly Region BeamBody = new("EvolutionBeamBody", new(0, 258, 2172, 204));
    internal static readonly Region BeamMouth = new("EvolutionBeamStart", new(204, 280, 900, 660));
    internal static readonly Region BeamTip = new("EvolutionBeamEnd", new(990, 370, 480, 210));
    internal static readonly Region Lance = new("EvolutionLance", new(220, 240, 1610, 330));
    internal static readonly Region Droplet = new("EvolutionDroplet", new(125, 450, 1010, 375));
    internal static readonly Region Spirit = new("EvolutionSpirit", new(195, 208, 1220, 573));
    internal static readonly Region Column = new("EvolutionColumn", new(0, 195, 2172, 330));
    internal static readonly Region Bomb = new("EvolutionBomb", new(165, 192, 950, 865));
    internal static readonly Region[] Regions = { BeamBody, BeamMouth, BeamTip, Lance, Droplet, Spirit, Column, Bomb };

    private static readonly Color Energy = new(240, 37, 73);
    private static readonly Color Hot = new(255, 165, 173);

    private static void Sprite(Region region, Vector2 center, Vector2 size, Vector2 pivot, float rotation, Color color,
        SpriteEffects effects = SpriteEffects.None)
    {
        Rectangle source = region.Area;
        Main.spriteBatch.Draw(EvolutionVisuals.Asset(region.Name), center - Main.screenPosition, source, color, rotation,
            source.Size() * pivot, size / source.Size(), effects, 0);
    }

    internal static void DrawLance(Vector2 center, float rotation, float opacity)
    {
        Sprite(Lance, center, new Vector2(70, 18), new Vector2(.66f, .5f), rotation, Energy * opacity);
        Sprite(Lance, center, new Vector2(70, 18), new Vector2(.66f, .5f), rotation,
            (Hot with { A = 0 }) * (opacity * .4f));
    }

    internal static void DrawDroplet(Vector2 center, float rotation, float opacity)
    {
        // Place the liquid bulb (not the center of its trailing silhouette) on the hitbox.
        Sprite(Droplet, center, new Vector2(42, 23), new Vector2(.8f, .5f), rotation, new Color(246, 65, 85) * opacity);
    }

    internal static void DrawSpirit(Vector2 center, float rotation, float age, float opacity)
    {
        float breathe = 1 + MathF.Sin(age * .19f) * .045f;
        Vector2 size = new Vector2(44, 28) * breathe;
        EvolutionVisuals.Glow(center, EvolutionVisuals.Blood * (opacity * .42f), new Vector2(43, 29), rotation);
        Sprite(Spirit, center, size, new Vector2(.82f, .55f), rotation, Energy * opacity);
        Sprite(Spirit, center, size, new Vector2(.82f, .55f), rotation,
            (EvolutionVisuals.Core with { A = 0 }) * (opacity * .65f));
    }

    internal static void DrawBomb(Vector2 center, float rotation, float size, float charge)
    {
        Sprite(Bomb, center, new Vector2(size), new Vector2(.5f), rotation, new Color(179, 27, 61));
        Sprite(Bomb, center, new Vector2(size), new Vector2(.5f), rotation,
            (Hot with { A = 0 }) * (.08f + charge * .65f));
    }

    internal static void DrawBeam(Vector2 start, Vector2 end, float age, float opacity, bool eruption = false)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < .5f || opacity <= .001f) return;
        Vector2 direction = delta / length;
        float rotation = direction.ToRotation();
        float width = (eruption ? 38 : 30) * (1 + MathF.Sin(age * .9f) * .035f);
        float capLength = Math.Min(26, length * .22f);
        Region bodyRegion = eruption ? Column : BeamBody;
        Rectangle body = bodyRegion.Area;

        // Tile in world space rather than stretching a tiny texture across a 2000-pixel beam.
        // Scrolling changes only UVs. Its core, silhouette center and endpoints never wander
        // away from the straight collision segment. Clip the last tile to the remaining length.
        float tileLength = eruption ? 180 : 240;
        float sourcePerPixel = body.Width / tileLength;
        float cursor = 0;
        int scroll = (int)(age * (eruption ? 30 : 55));
        int texel = scroll % body.Width;
        bool reverse = scroll / body.Width % 2 != 0;
        while (cursor < length - capLength)
        {
            float remaining = length - capLength - cursor;
            int count = Math.Min(body.Width - texel, Math.Max(1, (int)MathF.Ceiling(remaining * sourcePerPixel)));
            float span = Math.Min(remaining, count / sourcePerPixel);
            Vector2 point = start + direction * cursor;
            Vector2 middle = point + direction * (span * .5f) - Main.screenPosition;
            // Conservative rotated-tile bounds: long offscreen laser walls do not submit
            // thousands of invisible sprite layers every frame.
            float margin = span * .5f + width;
            if (middle.X >= -margin && middle.X <= Main.screenWidth + margin &&
                middle.Y >= -margin && middle.Y <= Main.screenHeight + margin)
            {
                // Mirrored repetition joins identical edge texels, avoiding hard vertical
                // seams even though the atlas was painted rather than mathematically tiled.
                int sourceX = body.X + (reverse ? body.Width - texel - count : texel);
                Rectangle sample = new(sourceX, body.Y, count, body.Height);
                SpriteEffects flip = reverse ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 size = new(span + .25f, width);
                Region strip = new(bodyRegion.Name, sample);
                Sprite(strip, point, size, new Vector2(0, .5f), rotation, Energy * opacity, flip);
                Sprite(strip, point, size, new Vector2(0, .5f), rotation,
                    (Hot with { A = 0 }) * (opacity * (eruption ? .2f : .45f)), flip);
                if (!eruption)
                {
                    // Use the texture's hot channel, not a solid rectangle, for the
                    // narrow luminous core. Its layer stays centered on the collision line.
                    Region core = new(bodyRegion.Name, new Rectangle(sourceX, 347, count, 27));
                    Sprite(core, point, new Vector2(size.X, 8), new Vector2(0, .5f), rotation,
                        (EvolutionVisuals.Core with { A = 0 }) * (opacity * .85f), flip);
                    EvolutionVisuals.Glow(point + direction * (span * .5f),
                        EvolutionVisuals.Blood * (opacity * .09f), new Vector2(span + 12, 49), rotation);
                }
            }
            cursor += span;
            texel = 0;
            reverse = !reverse;
        }

        // Separate ignition flash and tapered light tip, ending at the real beam endpoint.
        Sprite(BeamTip, end, new Vector2(capLength + 5, width), new Vector2(1, .5f), rotation, Energy * opacity);
        Sprite(BeamTip, end, new Vector2(capLength + 5, width), new Vector2(1, .5f), rotation,
            (Hot with { A = 0 }) * (opacity * .5f));
        Sprite(BeamMouth, start, new Vector2(eruption ? 60 : 52, eruption ? 52 : 42),
            new Vector2(.25f, .58f), rotation, Energy * opacity);
        EvolutionVisuals.Glow(start, EvolutionVisuals.Blood * (opacity * .35f), new Vector2(64, 45), rotation);
    }
}
