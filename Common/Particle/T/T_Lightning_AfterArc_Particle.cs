using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Passenger;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Lightning_AfterArc_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 pixels;
    private readonly SpriteEffects mirror;
    public override string TexturePath => "ArknightsMod/Content/Textures/PassengerArc";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override int FrameCount => 1;

    public T_Lightning_AfterArc_Particle(Vector2 position, Vector2 velocity, Vector2 direction,
        Color color, float length, float width, int lifetime)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        Lifetime = Math.Clamp(lifetime, 5, 22);
        Rotation = direction.SafeNormalize(Vector2.UnitY).ToRotation() - MathHelper.PiOver2;
        pixels = new Vector2(MathHelper.Clamp(width, 8f, 90f), MathHelper.Clamp(length, 12f, 180f));
        mirror = (Main.rand.NextBool() ? SpriteEffects.FlipHorizontally : SpriteEffects.None)
            | (Main.rand.NextBool() ? SpriteEffects.FlipVertically : SpriteEffects.None);
    }

    public override void Update() => Velocity *= 0.86f;

    public override void Draw()
    {
        float fade = MathF.Pow(1f - LifetimeRatio, 1.45f);
        Vector2 size = pixels * new Vector2(1f - LifetimeRatio * 0.30f, 1f - LifetimeRatio * 0.08f);
        Vector2 scale = size / Texture.Size();
        Vector2 origin = Texture.Size() * 0.5f;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * (fade * 0.88f),
            Rotation, origin, scale, mirror, 0f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null,
            Color.Lerp(Color, PassengerLightningVisuals.Core, 0.82f) * (fade * 0.28f), Rotation, origin, scale, mirror, 0f);
    }
}
