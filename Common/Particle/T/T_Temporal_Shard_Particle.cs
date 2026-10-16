using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Temporal_Shard_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color tint;
    private readonly float size;
    private readonly float spin;
    public override string TexturePath => MostimaVisuals.Assets + "TemporalStar";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Temporal_Shard_Particle(Vector2 position, Vector2 velocity, Color tint, int lifetime, float size)
    {
        Position = position; Velocity = velocity; this.tint = Color = tint;
        Lifetime = lifetime; this.size = size; spin = Main.rand.NextFloat(-.07f, .07f);
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update()
    {
        Velocity *= .955f;
        Rotation += spin;
        Color = tint * MathF.Sin(MathHelper.Pi * MathHelper.Clamp(Time / (float)Lifetime, 0f, 1f));
    }
    public override void Draw()
    {
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, new Vector2(size, size * .55f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
