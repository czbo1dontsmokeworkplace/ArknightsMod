using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Charge_Flash_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color tint;
    private readonly float size;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Charge_Flash_Particle(Vector2 center, Vector2 aim, Color tint, float strength)
    {
        Position = center; Rotation = aim.ToRotation(); this.tint = tint;
        size = strength * 78f; Lifetime = 10;
    }
    public override void Update() => Color = tint * MathF.Pow(1f - LifetimeRatio, 2f);
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 140f)) return;
        Vector2 point = Position + Rotation.ToRotationVector2() * size * .27f;
        for (int i = 0; i < 3; i++)
        {
            Vector2 dimensions = new(size * (1.2f - .3f * i), size * (.52f - i * .12f));
            Color color = Color.Lerp(tint, Microsoft.Xna.Framework.Color.White, i * .22f) * MathF.Pow(1f - LifetimeRatio, 2f);
            Main.spriteBatch.Draw(Texture, point - Main.screenPosition, null, color, Rotation,
                Texture.Size() * .5f, dimensions / Texture.Size(), SpriteEffects.None, 0f);
        }
    }
}
