using System;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// A physical fleck with angular drift, or a soft rolling remnant; both have bounded lives.
public sealed class T_Crusher_Fragment : Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => smoke ? BlendState.NonPremultiplied : BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly bool smoke;
    private readonly CrusherKind kind;
    private readonly float size, turn;
    private readonly Vector2 origin;
    public T_Crusher_Fragment(Vector2 point, Vector2 velocity, CrusherKind kind, int life, float size, bool smoke)
    {
        Position = origin = point; Velocity = velocity; this.kind = kind; this.smoke = smoke;
        Lifetime = Math.Clamp(life, 10, 40); this.size = Math.Clamp(size, 3, 48);
        Color = CrusherRules.Tint(kind); Rotation = velocity.ToRotation(); turn = Main.rand.NextFloat(-.075f, .075f);
    }
    public override void Update()
    {
        Velocity *= smoke ? .94f : .95f; Velocity = Velocity.RotatedBy(turn * .3f);
        Velocity.Y += smoke ? -.035f : kind == CrusherKind.Ulpianus ? .065f : .12f;
        Rotation += turn;
        if (Vector2.DistanceSquared(origin, Position + Velocity) > 220 * 220) Kill();
    }
    public override void Draw()
    {
        float t = LifetimeRatio, fade = Math.Min(1, Time / 3f) * MathF.Pow(1 - t, 1.4f);
        if (smoke) {
            ChargedOperatorVfx.Sprite("OrbitSmear", Position, new Vector2(size * (1 + t), size * .7f),
                Color.Lerp(new Color(19, 25, 38), Color, .22f) * (fade * .4f), Rotation);
            return;
        }
        ChargedOperatorVfx.Sprite(kind == CrusherKind.Ulpianus ? "ImpactPetals" : "ForwardFlame", Position,
            new Vector2(size * (1 - .45f * t), size * .45f), Color * fade, Rotation);
        ChargedOperatorVfx.Sprite("StarCore", Position, new Vector2(size * .28f), CrusherRules.Edge(kind) * (fade * .65f));
    }
}
