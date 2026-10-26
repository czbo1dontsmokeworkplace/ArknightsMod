using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Both strands share one short-lived particle. No extra projectiles or draw-state changes.
public sealed class T_Sniper_Flight_Helix : Particle
{
    private readonly Vector2 firstStart, firstEnd, secondStart, secondEnd;
    private readonly float depth;
    public override string TexturePath => "Terraria/Images/MagicPixel";

    public T_Sniper_Flight_Helix(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart,
        Vector2 secondEnd, Color theme, float depth)
    {
        Position = (firstEnd + secondEnd) * .5f;
        this.firstStart = firstStart - Position; this.firstEnd = firstEnd - Position;
        this.secondStart = secondStart - Position; this.secondEnd = secondEnd - Position;
        Color = theme; this.depth = depth; Lifetime = 7;
    }

    public override void Draw()
    {
        float fade = MathF.Pow(1 - Math.Clamp(LifetimeRatio, 0, 1), 1.7f);
        if (fade <= .01f) return;
        // A narrow dark edge keeps the pale strands readable against daylight.
        Color edge = Color.Lerp(Color.Black, Color, .16f) * (fade * .65f);
        Segment(firstStart, firstEnd, edge, 4f);
        Segment(secondStart, secondEnd, edge, 4f);
        Color first = Color.Lerp(Color, Color.White, .18f);
        Color second = Color.Lerp(Color, Color.White, .58f);
        // Swap foreground at crossings to preserve the winding, rather than a flat zigzag.
        if (depth > 0)
        {
            Strand(firstStart, firstEnd, first, -depth, fade);
            Strand(secondStart, secondEnd, second, depth, fade);
        }
        else
        {
            Strand(secondStart, secondEnd, second, depth, fade);
            Strand(firstStart, firstEnd, first, -depth, fade);
        }
    }

    private void Strand(Vector2 start, Vector2 end, Color tint, float depth, float fade)
    {
        float front = (depth + 1) * .5f;
        Segment(start, end, tint * (fade * MathHelper.Lerp(.5f, .9f, front)), MathHelper.Lerp(1.4f, 2.4f, front));
        Segment(start, end, Color.Lerp(tint, Color.White, .65f) * (fade * .65f), .8f);
    }

    private void Segment(Vector2 start, Vector2 end, Color tint, float width)
    {
        Vector2 delta = end - start;
        Main.spriteBatch.Draw(Texture, Position + start - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            tint, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length() + .5f, width), SpriteEffects.None, 0);
    }
}
