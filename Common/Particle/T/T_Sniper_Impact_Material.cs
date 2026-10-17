using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Independent accelerating/decelerating clumps carry the explosion beyond its first flash.
public sealed class T_Sniper_Impact_Grain : Particle
{
    private readonly float diameter, curl;
    private readonly Color tint;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Sniper_Impact_Grain(Vector2 center, Vector2 velocity, Color color, float size, int life, float curl)
    {
        Position = center; Velocity = velocity; tint = color; diameter = size; Lifetime = life; this.curl = curl;
    }
    public override void Update()
    {
        Velocity = Velocity.RotatedBy(curl * (1 - LifetimeRatio)) * .91f;
        Rotation = Velocity.ToRotation();
    }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1), fade = MathF.Pow(1 - t, 1.5f);
        Vector2 size = new Vector2(1.3f, 1) * diameter * (1 - t * .45f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint * fade * .55f,
            Rotation, Texture.Size() / 2, size * 2.4f / Texture.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint * fade,
            Rotation, Texture.Size() / 2, size / Texture.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color.Lerp(tint, Color.White, .8f) * fade,
            Rotation, Texture.Size() / 2, size * .35f / Texture.Size(), SpriteEffects.None, 0);
    }
}

public sealed class T_Sniper_Impact_Cloud : Particle
{
    private readonly Color tint;
    private readonly float diameter;
    public override string TexturePath => "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    public T_Sniper_Impact_Cloud(Vector2 center, Vector2 velocity, Color color, float size, int life, float angle)
    { Position = center; Velocity = velocity; tint = color; diameter = size; Lifetime = life; Rotation = angle; }
    public override void Update() { Velocity *= .94f; Velocity.Y -= .025f; Rotation += .015f; }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        float fade = Math.Min(1, (Time + 1) / 4f) * MathF.Pow(1 - t, 1.4f);
        Vector2 size = new Vector2(diameter) * (.55f + t * 1.1f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint * fade * .55f,
            Rotation, Texture.Size() / 2, size / Texture.Size(), SpriteEffects.None, 0);
    }
}
