using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Only this mod's ParticleManager registers these finite, moving fragments.
// PrettySparkleParticle and Dust remain in their own vanilla systems.
public sealed class T_Vanguard_Fragment_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly VanguardSpearKind kind;
    private readonly float spin;
    public T_Vanguard_Fragment_Particle(Vector2 position, Vector2 velocity, VanguardSpearKind kind, int lifetime)
    {
        Position = position; Velocity = velocity; this.kind = kind;
        Color = VanguardSpearRules.Tint(kind); Lifetime = Math.Clamp(lifetime, 8, 30);
        Rotation = velocity.ToRotation(); spin = Main.rand.NextFloat(-.12f, .12f);
    }
    public override void Update()
    {
        Velocity *= .94f;
        if (kind is VanguardSpearKind.WildMane or VanguardSpearKind.Bagpipe) Velocity.Y += .08f;
        Rotation += spin;
    }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - LifetimeRatio, 1.5f);
        Vector2 size = kind switch {
            VanguardSpearKind.Vigna => new(5, 3), VanguardSpearKind.Grani => new(8, 1.5f),
            VanguardSpearKind.WildMane => new(9, 3), _ => new(6, 2)
        };
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color * fade,
            Rotation, new Vector2(.5f), size * (1 - LifetimeRatio * .35f), SpriteEffects.None, 0);
    }
}
