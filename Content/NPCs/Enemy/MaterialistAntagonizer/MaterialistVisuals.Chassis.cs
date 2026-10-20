using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawArrival(MaterialistAntagonizer boss, Texture2D body, Vector2 position, float scale, float solid)
    {
        int t = boss.Timer;
        float reveal = MaterialistFormation.Smooth((t - 24) / 96f);
        float frameAlpha = MaterialistFormation.Smooth(t / 24f) * (1 - MaterialistFormation.Smooth((t - 178) / 32f));
        Vector2 size = body.Size() * scale + new Vector2(46, 32);
        Brackets(position, size, Cyan, frameAlpha * .9f);
        if (t < 24 || t >= 205) return;
        float hologramAlpha = (1 - solid * .8f) * frameAlpha;
        if (!MaterialistTechRenderer.ChassisMaterial(body, position, null, boss.NPC.rotation, new Vector2(scale),
            SpriteEffects.None, Cyan * hologramAlpha, 1, reveal))
        {
            int height = Math.Clamp((int)(body.Height * reveal), 0, body.Height);
            if (height > 0)
                Main.spriteBatch.Draw(body, position - Main.screenPosition, new Rectangle(0, 0, body.Width, height),
                    Cyan * (.8f * hologramAlpha), boss.NPC.rotation, body.Size() * .5f, scale, SpriteEffects.None, 0);
        }
        float y = MathHelper.Lerp(-body.Height * scale * .5f, body.Height * scale * .5f, reveal);
        Vector2 left = position + new Vector2(-size.X * .5f, y).RotatedBy(boss.NPC.rotation);
        Vector2 right = position + new Vector2(size.X * .5f, y).RotatedBy(boss.NPC.rotation);
        Line(left, right, (White with { A = 0 }) * frameAlpha, 3);
        Line(left - new Vector2(0, 4), right - new Vector2(0, 4), Cyan * (frameAlpha * .45f), 2);
        Glow((left + right) * .5f, Cyan * (.3f * frameAlpha), new Vector2(size.X, 18), boss.NPC.rotation);
    }

    private static void DrawCoreTelegraph(MaterialistAntagonizer boss)
    {
        if (boss.CoreSuppressed || !boss.EscortFire || !MaterialistTactics.Active(boss.Timer) || boss.Order == FlightOrder.Ram && boss.Timer >= 114) return;
        if (boss.ArtilleryActive) { DrawArtilleryBattery(boss); return; }
        int beat = MaterialistTactics.Beat(boss.Timer);
        Vector2 origin = boss.CoreMuzzle;
        if (beat < 24)
        {
            float charge = beat / 24f;
            Color color = Color.Lerp(Cyan, Amber, charge);
            int sides = boss.CorePlan is CorePattern.SquareLattice or CorePattern.Diamond ? 4 : 12;
            Ring(origin, 38 - charge * 12, color * (.4f + charge * .5f), boss.CorePlan == CorePattern.Diamond ? MathHelper.PiOver4 : 0, 2, 1, sides);
            Glow(origin, color * (.35f + charge * .5f), new Vector2(30 + charge * 28));
        }
        if (beat is >= 82 and < 106 || beat is >= 112 and < 128)
        {
            Vector2 axis = (boss.CoreAim - origin).SafeNormalize(Vector2.UnitY);
            int half = boss.Phase == 1 ? 1 : 2;
            for (int i = -half; i <= half; i++)
                Line(origin, origin + axis.RotatedBy(i * .105f * MaterialistRules.SpreadSpacing) * 850, Amber * .28f, 1);
            Reticle(boss.CoreAim, 19, Amber * .8f, boss.Timer);
            Glow(origin, White * .6f, new Vector2(32));
        }
    }
}
