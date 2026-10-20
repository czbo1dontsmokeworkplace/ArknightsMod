using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawVanillaRocket(MaterialistHazard h, Color accent)
    {
        Projectile p = h.Projectile;
        Main.instance.LoadProjectile(ProjectileID.RocketIII);
        Texture2D texture = TextureAssets.Projectile[ProjectileID.RocketIII].Value;
        int frames = Math.Max(1, Main.projFrames[ProjectileID.RocketIII]);
        Rectangle frame = texture.Frame(1, frames, 0, h.Age / 4 % frames);
        Vector2 center = p.Center - Main.screenPosition, origin = frame.Size() * .5f;
        float angle = p.velocity.ToRotation() + MathHelper.PiOver2;
        float scale = h.Kind == DroneShot.DropMissile ? 1.6f : 1.35f;
        for (int i = 0; i < 8; i++)
            Main.spriteBatch.Draw(texture, center + (i * MathHelper.TwoPi / 8).ToRotationVector2() * 2,
                frame, accent * (.7f * h.Visibility), angle, origin, scale, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(texture, center, frame, Color.White * h.Visibility, angle, origin, scale, SpriteEffects.None, 0);
        Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY), rear = p.Center - axis * frame.Height * scale * .35f;
        bool ignited = h.Kind != DroneShot.CommandRocket || h.Age - h.Delay >= 20;
        float flame = ignited ? 24 + MathF.Sin(h.Age * .6f) * 5 : 8;
        Glow(rear - axis * flame * .4f, accent * (ignited ? .55f : .2f), new Vector2(flame * 1.7f, 16), p.rotation);
        Glow(rear - axis * flame * .2f, White * (ignited ? .65f : .25f), new Vector2(flame, 7), p.rotation);
    }
}
