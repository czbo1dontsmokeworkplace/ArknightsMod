using System;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

internal static class MedicLightningVfx
{
    internal static readonly Color Emerald = new(55, 225, 132), Mint = new(216, 255, 221);
    internal static void Draw(MedicArt art, float opacity)
    {
        var q = art.Projectile;
        Vector2 axis = q.velocity.SafeNormalize(-Vector2.UnitY), side = new(-axis.Y, axis.X);
        Vector2 previous = q.Center;
        int phase = (int)art.Age / 3;
        for (int i = 0; i < 16; i++)
        {
            Vector2 point = q.Center - axis * (i + 1) * 11;
            float zig = i == 15 ? 0 : MathF.Sin((i * 17 + phase * 11 + q.identity * 7) * 1.73f) * (4 + (i % 3) * 2);
            point += side * zig;
            float fade = (1 - i / 17f) * opacity;
            MedicArtsVfx.Thread(previous, point, Emerald, 3.2f, fade * .8f);
            Segment(previous, point, Mint, 1.4f, fade);
            if (i % 4 == 2)
                Segment(point, point + side * ((i % 2 == 0 ? 1 : -1) * 17) - axis * 13, Emerald, .8f, fade * .65f);
            previous = point;
        }
        // A split spearhead and two orbiting electrodes, rather than a straight beam cap.
        foreach (int sign in new[] { -1, 1 })
        {
            Segment(q.Center + axis * 15, q.Center - axis * 10 + side * (sign * 7), Mint, 1.5f, opacity);
            MedicArtsVfx.Glow(q.Center - axis * 14 + side * (sign * (8 + MathF.Sin(art.Age * .25f) * 2)), new Vector2(5), Emerald, opacity * .6f);
        }
        MedicArtsVfx.Glow(q.Center, new Vector2(24, 12), Emerald, opacity * .55f, axis.ToRotation());
    }
    private static void Segment(Vector2 from, Vector2 to, Color color, float width, float opacity)
    {
        Vector2 delta = to - from;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            MedicArtsVfx.Light(color, opacity), delta.ToRotation(), new Vector2(0, .5f),
            new Vector2(delta.Length(), width), SpriteEffects.None);
    }
    internal static void Flight(MedicArt art)
    {
        if (Main.dedServ) return;
        var q = art.Projectile;
        if ((int)art.Age % 2 == 0)
            new T_Medic_Item_Spark(q.Center, -q.velocity * .08f + Main.rand.NextVector2Circular(.4f, .4f), art.ProfileId, Mint, true, 16, 3).Spawn();
        if ((int)art.Age % 3 == 0)
        {
            var dust = Dust.NewDustPerfect(q.Center, DustID.TintableDustLighted, -q.velocity * .05f, 150, Emerald, .75f);
            dust.noGravity = true;
        }
    }
}
