using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawGravityStreak(MaterialistHazard hazard)
    {
        Projectile p = hazard.Projectile;
        Texture2D texture = Asset(MaterialistAntagonizer.AssetRoot + "GravityStreak");
        Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY);
        float length = MathHelper.Clamp(38 + p.velocity.Length() * 4, 44, 88);
        float fade = hazard.Visibility;
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                void Draw(Vector2 center, Vector2 direction, float width, float reach, Color tint) =>
                    Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, tint,
                        direction.ToRotation() + MathHelper.PiOver2, texture.Size() * .5f,
                        new Vector2(width / texture.Width, reach / texture.Height), SpriteEffects.None, 0);
                if (!Reduced)
                    for (int i = 6; i >= 2; i -= 2)
                        if (p.oldPos[i] != Vector2.Zero && p.oldPos[i - 1] != Vector2.Zero)
                            Draw(p.oldPos[i] + p.Size * .5f, (p.oldPos[i - 1] - p.oldPos[i]).SafeNormalize(axis),
                                9, length * .75f, new Color(77, 221, 123) * ((1 - i / 8f) * .28f * fade));
                Vector2 center = p.Center - axis * (length * .18f);
                Draw(center, axis, 27, length * 1.1f, new Color(54, 201, 109) * (.45f * fade));
                Draw(center, axis, 16, length, new Color(145, 255, 168) * (.85f * fade));
                Draw(center, axis, 7, length * .78f, new Color(238, 255, 231) * fade);
            }
            finally { Main.spriteBatch.End(); }
        }
        finally
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
