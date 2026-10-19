using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_ArtilleryMuzzleFlame : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color outer, core;
    private readonly float length;
    public override string TexturePath => "ArknightsMod/Content/Textures/OperatorVfx/ForwardFlame";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    internal T_ArtilleryMuzzleFlame(Vector2 position, Vector2 direction, Color outer, Color core, float length)
    {
        Position = position; Rotation = direction.ToRotation() + MathHelper.PiOver2;
        this.outer = outer; this.core = core; this.length = MathHelper.Clamp(length, 48f, 100f);
        Lifetime = 8;
    }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = MathF.Pow(1f - t, 1.5f);
        Vector2 origin = new(Texture.Width * .5f, Texture.Height * .92f);
        Vector2 size = new(26f * (1f - t * .45f), length * (1f - t * .4f));
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, outer * (.85f * fade), Rotation,
            origin, size / Texture.Size(), SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, core * fade, Rotation,
            origin, size * new Vector2(.48f, .63f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}

public sealed class T_ArtilleryMuzzleRing : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color tint;
    private readonly float diameter;
    public override string TexturePath => "ArknightsMod/Content/Textures/circle_03";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    internal T_ArtilleryMuzzleRing(Vector2 position, Vector2 axis, Color tint, float diameter, int lifetime)
    {
        Position = position; Rotation = axis.ToRotation(); Velocity = axis * 1.7f;
        this.tint = tint; this.diameter = MathHelper.Clamp(diameter, 32f, 90f); Lifetime = lifetime;
    }
    public override void Update() => Velocity *= .94f;
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float size = MathHelper.Lerp(8f, diameter, 1f - MathF.Pow(1f - t, 3f));
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null,
            tint * (.68f * MathF.Pow(1f - t, 1.25f)), Rotation, Texture.Size() * .5f,
            new Vector2(size * .32f, size) / Texture.Size(), SpriteEffects.None, 0f);
    }
}

public sealed class T_ArtilleryMuzzleSmoke : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float diameter;
    public override string TexturePath => "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    public override BlendState DrawBlendState => BlendState.NonPremultiplied;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    internal T_ArtilleryMuzzleSmoke(Vector2 position, Vector2 velocity, float diameter, int lifetime)
    {
        Position = position; Velocity = velocity; this.diameter = diameter; Lifetime = lifetime;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update() { Velocity *= .91f; Velocity.Y -= .015f; Rotation += .025f; }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float opacity = Math.Min(1f, t * 6f) * MathF.Pow(1f - t, 1.4f) * .28f;
        float size = diameter * (.65f + t * .85f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, new Color(143, 126, 104) * opacity,
            Rotation, Texture.Size() * .5f, new Vector2(size, size * .8f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
