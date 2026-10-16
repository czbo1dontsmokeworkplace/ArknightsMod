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

public sealed class T_Wave_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float diameter;
    private readonly Color tint;
    public override string TexturePath => OperatorFlightVfx.Assets + "RuptureRing";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Wave_Particle(Vector2 position, Color tint, float diameter, int life)
    { Position = position; Color = this.tint = tint; this.diameter = MathHelper.Clamp(diameter, 12f, 260f); Lifetime = life; }
    public override void Update() { Rotation += .045f; Color = tint * MathF.Pow(1f - LifetimeRatio, 1.8f); }
    public override void Draw() => Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color,
        Rotation, Texture.Size() * .5f, new Vector2(diameter * (.2f + .8f * MathF.Sqrt(LifetimeRatio))) / Texture.Size(), SpriteEffects.None, 0f);
}
