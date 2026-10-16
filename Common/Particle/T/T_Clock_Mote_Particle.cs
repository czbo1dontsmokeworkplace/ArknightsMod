using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Clock_Mote_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 center;
    private readonly Color tint;
    private readonly float radius;
    private readonly int spin;
    private float angle;
    public override string TexturePath => MostimaVisuals.Assets + "TemporalOrb";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Clock_Mote_Particle(Vector2 center, float radius, float angle, int spin, Color tint)
    {
        Position = this.center = center;
        this.radius = radius;
        this.angle = angle;
        this.spin = spin;
        this.tint = Color = tint;
        Lifetime = 42;
        Direction = 1;
    }
    public override void Update()
    {
        angle += .052f * spin;
        float swell = .88f + .12f * MathF.Sin(Time * .28f + angle);
        Position = center + angle.ToRotationVector2() * radius * swell;
        Color = tint * MathF.Min(1f, (Lifetime - Time) / 12f);
    }
    public override void Draw()
    {
        float diameter = spin > 0 ? 8f : 6f;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, 0f,
            Texture.Size() * .5f, new Vector2(diameter) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
