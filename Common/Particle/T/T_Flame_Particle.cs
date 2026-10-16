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

public sealed class T_Flame_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color body, core;
    private readonly float size;
    public override string TexturePath => OperatorFlightVfx.Assets + "ForwardFlame";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Flame_Particle(Vector2 position, Vector2 velocity, Color body, Color core, float size, int life)
    {
        Position = position; Velocity = velocity; this.body = body; this.core = core;
        this.size = MathHelper.Clamp(size, 8f, 38f); Lifetime = Math.Clamp(life, 8, 30);
        Rotation = velocity.ToRotation() + MathHelper.PiOver2;
    }
    public override void Update()
    {
        Velocity *= .94f;
        Color = Color.Lerp(core, body, LifetimeRatio) * MathF.Pow(1f - LifetimeRatio, 1.3f);
    }
    public override void Draw() => Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color,
        Rotation, Texture.Size() * .5f,
        new Vector2(size * .48f * (1f - LifetimeRatio * .6f) / Texture.Width, size * (1f - LifetimeRatio * .4f) / Texture.Height), SpriteEffects.None, 0f);
}
