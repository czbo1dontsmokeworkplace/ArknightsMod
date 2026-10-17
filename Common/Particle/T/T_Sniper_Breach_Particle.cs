using System;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Common.Particle.T;

// Impact-only composition: stellar shards, gauss forward spray, delayed pressure rings,
// and an original operator-specific fracture. The particle manager owns every Begin/End.
public sealed class T_Sniper_Breach_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly ChargedOperatorKind kind;
    private readonly bool dark;
    private readonly float strength, seed;
    public override string TexturePath => "ArknightsMod/Content/Textures/ChargedSniper/BreachRing";
    public override BlendState DrawBlendState => dark ? BlendState.AlphaBlend : BlendState.Additive;
    public T_Sniper_Breach_Particle(Vector2 center, Vector2 direction, ChargedOperatorKind kind, bool heavy, bool critical, bool dark, float seed)
    {
        Position = center; Rotation = direction.ToRotation(); this.kind = kind; this.dark = dark; this.seed = seed;
        Color = ChargedOperatorRules.ColorFor(kind);
        strength = (heavy ? 1.6f : .85f) * (critical ? 1.15f : 1);
        Lifetime = heavy ? 34 : 25;
    }
    public override void Update() { } // Stationary impact, all motion is bounded relative to its origin.
    public override void Draw()
    {
        if (dark) return;
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        float expand = 1 - MathF.Pow(1 - t, 3);
        float fade = MathF.Pow(1 - t, 1.8f);
        // Thick textured pressure fronts, without procedural sticks or polygon outlines.
        for (int layer = 0; layer < 3; layer++)
        {
            float delay = layer * .09f;
            if (t < delay) continue;
            float age = (t - delay) / (1 - delay);
            Vector2 size = new Vector2(90 + layer * 30, 145 + layer * 35) * strength * (.2f + .8f * (1 - MathF.Pow(1 - age, 3)));
            Vector2 offset = new Vector2(layer * 11 * strength, 0).RotatedBy(Rotation);
            Main.spriteBatch.Draw(Texture, Position + offset - Main.screenPosition, null,
                Color.Lerp(Color, Color.White, .35f) * MathF.Pow(1 - age, 2) * .8f,
                Rotation + layer * .15f, Texture.Size() / 2, size / Texture.Size(), SpriteEffects.None, 0);
        }
        // Overlapping lobes form a rounded explosion crown rather than an angular fracture.
        for (int i = 0; i < 7; i++)
        {
            float angle = i * MathHelper.TwoPi / 7 + seed;
            Vector2 offset = new Vector2(MathF.Cos(angle) * 26, MathF.Sin(angle) * 34) * expand * strength;
            Vector2 size = new Vector2(60, 74) * strength * (1 + expand * .6f);
            Main.spriteBatch.Draw(Texture, Position + offset.RotatedBy(Rotation) - Main.screenPosition, null,
                Color * fade * .26f, angle, Texture.Size() / 2, size / Texture.Size(), SpriteEffects.None, 0);
        }
    }
}
public sealed class T_Sniper_Breach_Flash : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float strength;
    public override string TexturePath => "ArknightsMod/Content/Textures/ChargedSniper/BreachStar";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Sniper_Breach_Flash(Vector2 center, Vector2 aim, Color color, bool heavy)
    {
        Position = center; Rotation = aim.ToRotation(); Color = color;
        strength = heavy ? 1.65f : .85f; Lifetime = 12;
    }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - Math.Clamp(LifetimeRatio, 0, 1), 2);
        Vector2 size = new Vector2(240, 115) * strength * (.55f + .45f * fade);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * fade * .85f, Rotation,
            Texture.Size() / 2, size / Texture.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color.White * fade, Rotation + .22f,
            Texture.Size() / 2, size * new Vector2(.65f, .85f) / Texture.Size(), SpriteEffects.None, 0);
    }
}
