using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Charge_Pulse_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float strength;
    private readonly bool release;
    private readonly Color tint;
    public override string TexturePath => OperatorFlightVfx.Assets + "RuptureRing";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Charge_Pulse_Particle(Vector2 center, Vector2 aim, Color tint, float strength, bool release)
    {
        Position = center; Rotation = aim.ToRotation(); this.tint = tint;
        this.strength = strength; this.release = release; Lifetime = release ? 18 : 13;
    }
    public override void Update() => Color = tint * MathF.Pow(1f - LifetimeRatio, 1.5f);
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 140f)) return;
        float opening = 1f - MathF.Pow(1f - LifetimeRatio, 4f);
        float diameter = MathHelper.Lerp(16f, release ? 100f : 76f, opening) * strength;
        Vector2 aim = Rotation.ToRotationVector2();
        for (int i = 0; i < (release ? 2 : 1); i++)
        {
            Vector2 point = Position + aim * (release ? opening * (18f + i * 22f) : 0f);
            Vector2 size = new Vector2(diameter * .38f, diameter) * (1f - i * .22f);
            Main.spriteBatch.Draw(Texture, point - Main.screenPosition, null, Color * (1f - i * .35f), Rotation,
                Texture.Size() * .5f, size / Texture.Size(), SpriteEffects.None, 0f);
        }
    }
}
