using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_LinearSpark_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float length, width;
    private readonly Color tint;
    public override string TexturePath => OperatorFlightVfx.Assets + "LinearSpark";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_LinearSpark_Particle(Vector2 position, Vector2 velocity, Color tint, float length, int life, float width)
    {
        Position = position; Velocity = velocity; Color = this.tint = tint;
        this.length = MathHelper.Clamp(length, 4f, 44f); this.width = MathHelper.Clamp(width, 1f, 9f);
        Lifetime = Math.Clamp(life, 6, 30); Rotation = velocity.ToRotation() + MathHelper.PiOver2;
    }
    public override void Update() { Velocity *= .965f; Color = tint * MathF.Pow(1f - LifetimeRatio, 1.4f); }
    public override void Draw() => Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color,
        Rotation, Texture.Size() * .5f, new Vector2(width / Texture.Width, length * (1f - LifetimeRatio * .65f) / Texture.Height), SpriteEffects.None, 0f);
}
