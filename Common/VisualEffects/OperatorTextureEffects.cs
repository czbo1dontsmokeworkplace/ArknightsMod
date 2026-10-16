using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

internal static class OperatorTextureEffects
{
    internal const string GlowPath = "ArknightsMod/Common/Particle/DefaultParticle";
    internal const string SmokePath = "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    internal static void Glow(Vector2 center, float diameter, Color color, float opacity)
    {
        if (Main.dedServ || !float.IsFinite(diameter) || diameter <= 0 || diameter > 240f) return;
        Texture2D texture = ModContent.Request<Texture2D>(GlowPath).Value;
        Color light = new Color(color.R, color.G, color.B, 0) * MathHelper.Clamp(opacity, 0f, 1f);
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, light, 0f, texture.Size() * .5f,
            new Vector2(diameter) / texture.Size(), SpriteEffects.None);
    }
    internal static void Cloud(Vector2 center, float diameter, Color color, float opacity, float rotation)
    {
        if (Main.dedServ || !float.IsFinite(diameter) || diameter <= 0 || diameter > 180f) return;
        Texture2D texture = ModContent.Request<Texture2D>(SmokePath).Value;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color * MathHelper.Clamp(opacity, 0f, 1f),
            rotation, texture.Size() * .5f, new Vector2(diameter) / texture.Size(), SpriteEffects.None);
    }
    internal static void Mote(Vector2 center, Vector2 velocity, Color color, float diameter, int lifetime = 24)
    {
        if (Main.dedServ) return;
        new T_Streak_Particle(center, velocity, Math.Clamp(lifetime, 8, 45), MathHelper.Clamp(diameter, 3f, 24f) / 64f,
            color, true) { Deformation = Vector2.One }.Spawn();
    }
    internal static void Puff(Vector2 center, Vector2 velocity, Color color, float diameter, int lifetime = 30)
    {
        if (!Main.dedServ) new T_SoftSmoke_Particle(center, velocity, color, diameter, lifetime).Spawn();
    }
}
