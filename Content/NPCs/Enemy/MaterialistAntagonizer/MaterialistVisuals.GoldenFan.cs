using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawGoldenFan(MaterialistHazard hazard)
    {
        Projectile p = hazard.Projectile;
        Texture2D gleam = Asset(MaterialistAntagonizer.AssetRoot + "GoldenFanGleam");
        Vector2 origin = gleam.Size() * .5f;
        Color gold = new(255, 202, 88, 0);
        float opacity = hazard.Visibility;
        float rotation = p.velocity.ToRotation() + MathHelper.PiOver2;
        void Stamp(Vector2 point, float fade, float scale)
            => Main.spriteBatch.Draw(gleam, point - Main.screenPosition, null, gold * (fade * opacity),
                rotation, origin, new Vector2(24, 36) / gleam.Size() * scale, SpriteEffects.None, 0);
        int count = Reduced ? 4 : 7;
        for (int i = count; i >= 1; i--)
        {
            if (p.oldPos[i] == Vector2.Zero || p.oldPos[i - 1] == Vector2.Zero) continue;
            Vector2 previous = p.oldPos[i] + p.Size * .5f, next = p.oldPos[i - 1] + p.Size * .5f;
            float fade = (1 - i / (float)(count + 1)) * .42f;
            Stamp(previous, fade, .8f);
            if (!Reduced) Stamp(Vector2.Lerp(previous, next, .5f), fade, .8f);
        }
        Stamp(p.Center, .85f, 1);
        Main.spriteBatch.Draw(gleam, p.Center - Main.screenPosition, null,
            new Color(255, 247, 211, 0) * opacity, rotation, origin,
            new Vector2(11, 25) / gleam.Size(), SpriteEffects.None, 0);
    }
}
