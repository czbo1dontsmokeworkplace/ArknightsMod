using System;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Small non-additive flight material. Independent of the much larger impact clouds.
public sealed class T_Sniper_Flight_Wisp : Particle
{
    private readonly Vector2 size;
    public override string TexturePath => "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    public T_Sniper_Flight_Wisp(Vector2 center, Vector2 velocity, float rotation, ChargedOperatorKind kind, float variation)
    {
        Position = center; Velocity = velocity; Rotation = rotation;
        Color = Color.Lerp(Color.Black, ChargedOperatorRules.ColorFor(kind), .1f);
        size = kind switch {
            ChargedOperatorKind.Andreana => new Vector2(23, 13),
            ChargedOperatorKind.Firewatch => new Vector2(27, 8),
            _ => new Vector2(20, 10)
        };
        Scale = .8f + variation * .4f;
        Lifetime = kind == ChargedOperatorKind.Andreana ? 18 : 13;
    }
    public override void Update()
    {
        Velocity *= .9f;
    }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * MathF.Pow(1 - t, 1.5f) * .85f,
            Rotation, Texture.Size() / 2, size * (Scale * (1 + t * .45f)) / Texture.Size(), SpriteEffects.None, 0);
    }
}
