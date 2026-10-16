using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Ready_Ripple_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color initialColor;
    private readonly float finalDiameter;
    public override string TexturePath => "ArknightsMod/Content/Textures/circle_03";
    public override BlendState DrawBlendState => BlendState.Additive;

    public T_Ready_Ripple_Particle(Vector2 position, Color color, int lifetime, float finalDiameter)
    {
        Position = position;
        initialColor = Color = color;
        Lifetime = lifetime;
        this.finalDiameter = finalDiameter;
    }

    public override void Update()
    {
        Color = initialColor * MathF.Pow(1f - LifetimeRatio, 1.35f);
        Rotation += .045f;
    }

    public override void Draw()
    {
        float diameter = MathHelper.Lerp(17f, finalDiameter, 1f - MathF.Pow(1f - LifetimeRatio, 2f));
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, diameter / Texture.Width, SpriteEffects.None, 0f);
    }
}
