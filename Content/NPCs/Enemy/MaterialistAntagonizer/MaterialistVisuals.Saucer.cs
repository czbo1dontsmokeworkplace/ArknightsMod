using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawSaucerRound(MaterialistHazard hazard)
    {
        Projectile p = hazard.Projectile;
        Main.instance.LoadProjectile(ProjectileID.SaucerLaser);
        Texture2D texture = TextureAssets.Projectile[ProjectileID.SaucerLaser].Value;
        Vector2 origin = texture.Size() * .5f;
        float rotation = p.rotation + MathHelper.PiOver2, fade = hazard.Visibility;
        Color tint = AmmoColor(hazard);
        Glow(p.Center, tint * (.32f * fade), new Vector2(19, 32), p.rotation - MathHelper.PiOver2);
        if (!Reduced)
            for (int i = 12; i >= 3; i -= 3)
                if (p.oldPos[i] != Vector2.Zero)
                    Main.spriteBatch.Draw(texture, p.oldPos[i] + p.Size * .5f - Main.screenPosition, null,
                        (tint with { A = 0 }) * ((1 - i / 15f) * .25f * fade), rotation,
                        origin, 1f, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(texture, p.Center - Main.screenPosition, null, Color.White * fade,
            rotation, origin, 1.15f, SpriteEffects.None, 0);
    }

    private static void DrawSaucerBattery(MaterialistAntagonizer boss)
    {
        int beat = boss.CoreBeat;
        if (beat < 8 || beat > 132) return;
        float charge = MathHelper.Clamp((beat - 8) / 40f, 0, 1);
        Color tint = new(255, 232, 112);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 muzzle = boss.SaucerPort(side);
            Vector2 axis = (boss.CoreSpreadAim - muzzle).SafeNormalize(Vector2.UnitY);
            Glow(muzzle, tint * (beat < 48 ? .2f + charge * .45f : .55f), new Vector2(26, 20));
            if (beat >= 48) continue;
            // Warn the actual wide fan, without circular decorations attached to the airframe.
            for (int lane = -1; lane <= 1; lane++)
            {
                Vector2 ray = axis.RotatedBy(lane * .85f);
                for (float along = 24; along < 520; along += 48)
                    Line(muzzle + ray * along, muzzle + ray * Math.Min(520, along + 16),
                        tint * ((lane == 0 ? .18f : .35f) * charge), 1);
            }
        }
    }
}
