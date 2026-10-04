using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;

// Deliberately restrained, asset-free placeholders. No shaders, full-screen flashes or camera shake.
internal static class DuoVisuals
{
    internal static void Line(SpriteBatch batch, Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 delta = to - from;
        if (delta.LengthSquared() < .001f) return;
        batch.Draw(TextureAssets.MagicPixel.Value, from, null, color, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
    internal static void Ring(SpriteBatch batch, Vector2 center, float radius, Color color, int segments = 24)
    {
        for (int i = 0; i < segments; i++)
        {
            Vector2 a = center + (MathHelper.TwoPi * i / segments).ToRotationVector2() * radius;
            Vector2 b = center + (MathHelper.TwoPi * (i + 1) / segments).ToRotationVector2() * radius;
            Line(batch, a, b, color, 1.4f);
        }
    }
    internal static void Person(SpriteBatch batch, NPC npc, Vector2 screen, Color tint, bool crossbow, float bulk = 1)
    {
        // Vanilla humanoid as a silhouette, overpainted with role-specific equipment.
        Texture2D body = TextureAssets.Npc[npc.type].Value;
        Rectangle frame = new(0, 0, body.Width, body.Height / Main.npcFrameCount[NPCID.Guide]);
        Vector2 feet = npc.Bottom - screen;
        batch.Draw(body, feet, frame, tint, 0, new Vector2(frame.Width * .5f, frame.Height),
            new Vector2(bulk, 1.05f), npc.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        Vector2 hand = npc.Center - screen + new Vector2(npc.direction * 13, -4);
        if (crossbow)
        {
            Line(batch, hand - new Vector2(12, 0), hand + new Vector2(18, 0), Color.LightGray * (tint.A / 255f), 3);
            Line(batch, hand + new Vector2(6, -13), hand + new Vector2(6, 13), tint, 4);
        }
        else
        {
            Line(batch, hand + new Vector2(0, -24), hand + new Vector2(0, 24), tint, 3);
            Ring(batch, hand + new Vector2(0, -24), 5, tint, 12);
        }
    }
    internal static void Smoke(Vector2 position, int count = 18)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < count; i++)
            Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(15, 24), DustID.Smoke,
                Main.rand.NextVector2Circular(2, 2), 140, new Color(38, 28, 48), 1.2f).noGravity = true;
    }
    internal static void Puff(Vector2 position, Color color, int count = 8)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < count; i++)
            Dust.NewDustPerfect(position, DustID.Cloud, Main.rand.NextVector2Circular(2, 2), 130, color, .75f).noGravity = true;
    }
}
