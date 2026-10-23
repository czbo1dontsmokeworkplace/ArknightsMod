using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistInstrument
{
    private static void Segment(Texture2D texture, Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 delta = to - from;
        Main.spriteBatch.Draw(texture, (from + to) * .5f - Main.screenPosition, null, color,
            delta.ToRotation() + MathHelper.PiOver2, texture.Size() * .5f,
            new Vector2(width / texture.Width, (delta.Length() + 10) / texture.Height), SpriteEffects.None, 0);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float opacity = Opacity;
        bool warning = Age < Delay;
        Color cyan = new(125, 238, 255, 0);
        Vector2 node = Lightning ? End : Start;
        float charge = MathHelper.Clamp(Age / (float)Math.Max(1, Delay), 0, 1);
        MaterialistVisuals.Brackets(node, new Vector2(36 - charge * 10), new Color(125, 238, 255), opacity * (.5f + charge * .5f));
        if (warning)
        {
            Vector2 axis = (End - Start).SafeNormalize(Vector2.UnitY);
            if (Lightning)
            {
                // The strike marker never moves after sampling the target area.
                MaterialistVisuals.Brackets(End, new Vector2(52), new Color(178, 171, 255), opacity * .5f);
                for (int i = 0; i < 7; i++)
                {
                    Vector2 point = Vector2.Lerp(End - Vector2.UnitY * 210, End, i / 6f);
                    MaterialistVisuals.Line(point, point + Vector2.UnitY * 9, cyan * (.22f + charge * .3f), 2);
                }
            }
            else
            {
                float reach = Vector2.Distance(Start, End);
                for (float along = 20; along < reach; along += 42)
                    MaterialistVisuals.Line(Start + axis * along, Start + axis * Math.Min(reach, along + 18),
                        cyan * ((.25f + charge * .45f) * opacity), 1.5f);
                MaterialistVisuals.Brackets(Mark, new Vector2(26), Color.LightCyan, opacity * .8f);
            }
            MaterialistVisuals.Glow(node, Color.LightCyan * ((.18f + charge * .2f) * opacity), new Vector2(34));
            return false;
        }
        Texture2D beam = TextureAssets.Extra[98].Value;
        if (!Lightning)
        {
            Vector2 axis = (End - Start).SafeNormalize(Vector2.UnitY);
            float reach = Vector2.Distance(Start, End);
            // Overlapping native light sprites form a white-hot cyan pulse with a narrow afterimage.
            float width = (Age - Delay < 2 ? 38 : 24) * Math.Max(.1f, opacity);
            for (float along = 0; along < reach; along += 96)
            {
                Vector2 a = Start + axis * Math.Max(0, along - 36);
                Vector2 b = Start + axis * Math.Min(reach, along + 150);
                Segment(beam, a, b, cyan * (.48f * opacity), width * 2.4f);
                Segment(beam, a, b, new Color(225, 255, 255, 0) * opacity, width);
            }
            MaterialistVisuals.Line(Start, End, new Color(255, 255, 255, 0) * opacity, Math.Max(1, 7 * opacity));
            MaterialistVisuals.Glow(Start, Color.LightCyan * opacity, new Vector2(72));
        }
        else
        {
            DrawStormArcs(opacity);
            MaterialistVisuals.Glow(End, Color.LightCyan * opacity, new Vector2(68, 28));
        }
        return false;
    }
}
