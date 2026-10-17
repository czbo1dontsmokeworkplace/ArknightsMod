using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Filled moving silhouettes: petals, shield crystals, lance teeth and brass shrapnel.
public sealed class T_Vanguard_Motif_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly VanguardSpearKind kind;
    private readonly Vector2 origin;
    private readonly float extent, spin;
    public T_Vanguard_Motif_Particle(Vector2 position, Vector2 velocity, VanguardSpearKind kind, float extent, int life)
    {
        Position = origin = position; Velocity = velocity; this.kind = kind;
        this.extent = Math.Clamp(extent, 4, 18); Lifetime = Math.Clamp(life, 10, 32);
        Color = VanguardSpearRules.Tint(kind); Rotation = velocity.ToRotation();
        spin = kind == VanguardSpearKind.Grani ? .02f : Main.rand.NextFloat(-.13f, .13f);
    }
    public override void Update()
    {
        Velocity *= kind == VanguardSpearKind.Grani ? .92f : .96f;
        if (kind != VanguardSpearKind.Grani) Velocity.Y += .1f;
        Rotation += spin;
        if (Vector2.DistanceSquared(Position + Velocity, origin) > 280 * 280)
        { Velocity = Vector2.Zero; Kill(); }
    }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - LifetimeRatio, 1.4f);
        Vector2 axis = Rotation.ToRotationVector2();
        float length = extent * (1 - LifetimeRatio * .35f);
        // Eight explicit one-pixel slices form a solid silhouette, rather than a stretched white line.
        for (int i = 0; i < 8; i++)
        {
            float t = (i + .5f) / 8;
            float profile = kind switch {
                VanguardSpearKind.Vigna => MathF.Sin(t * MathHelper.Pi) * .55f,
                VanguardSpearKind.Grani => (1 - Math.Abs(t * 2 - 1)) * .75f,
                VanguardSpearKind.WildMane => (1 - t) * .46f,
                _ => .32f + .12f * MathF.Sin(t * 5)
            };
            Color tint = Color.Lerp(Color * .45f, Color, t) * fade;
            Main.spriteBatch.Draw(Texture, Position + axis * ((t - .5f) * length) - Main.screenPosition,
                new Rectangle(0, 0, 1, 1), tint, Rotation, new Vector2(.5f),
                new Vector2(length / 8 + .3f, Math.Max(.6f, length * profile)), SpriteEffects.None, 0);
        }
    }
}
