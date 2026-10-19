using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Bounded contact volumes. This uses our existing neutral bloom mask, with an
// expanding tinted shell and a shorter warm core rather than vanilla dust.
public sealed class T_BlastCasterFluxBloom : Particle
{
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly Color tint;
    private readonly Vector2 size;
    private readonly bool contact;
    internal T_BlastCasterFluxBloom(Vector2 at, Vector2 velocity, Color tint, Vector2 size, int life, bool contact)
    {
        Position = at; Velocity = velocity; this.tint = tint; this.size = size;
        Lifetime = life; this.contact = contact; Rotation = velocity.LengthSquared() > .01f ? velocity.ToRotation() : 0;
    }
    public override void Update() { Velocity *= .91f; }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1), fade = MathF.Pow(1 - t, 1.7f);
        Vector2 extent = size * (contact ? .8f + t * .75f : 1 - t * .45f);
        Color shell = tint; shell.A = (byte)(fade * (contact ? .52f : .4f) * 255);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, shell, Rotation,
            Texture.Size() / 2, extent / Texture.Size(), SpriteEffects.None, 0);
        Color core = Color.Lerp(tint, new Color(255, 249, 225), .65f);
        core.A = (byte)(MathF.Pow(1 - t, 3) * .68f * 255);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, core, Rotation,
            Texture.Size() / 2, extent * .44f / Texture.Size(), SpriteEffects.None, 0);
    }
}

public sealed class T_BlastCasterFluxShard : Particle
{
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly Color tint;
    private readonly float size, turn;
    internal T_BlastCasterFluxShard(Vector2 at, Vector2 velocity, Color tint, float size, int life)
    {
        Position = at; Velocity = velocity; this.tint = tint; this.size = size; Lifetime = life;
        turn = Main.rand.NextFloat(-.045f, .045f); Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity = Velocity.RotatedBy(turn) * .94f; Velocity.Y -= .025f;
        Rotation = Velocity.ToRotation();
    }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        Color color = Color.Lerp(tint, new Color(255, 240, 198), .4f);
        color.A = (byte)((1 - t) * (1 - t) * 200);
        Vector2 extent = new(size * (1.2f + Velocity.Length() * .25f), size * .4f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, color, Rotation,
            Texture.Size() / 2, extent * (1 - t * .6f) / Texture.Size(), SpriteEffects.None, 0);
    }
}
