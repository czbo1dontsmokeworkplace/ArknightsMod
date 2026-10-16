using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Orbit_Glyph_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 center;
    private readonly float radius;
    private readonly float phase;
    private readonly float spin;
    private readonly Color tint;
    private readonly int glyph;
    public override string TexturePath => MostimaVisuals.Assets + "ChronoSigilV2";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Orbit_Glyph_Particle(Vector2 center, float radius, float phase, float spin, Color tint, int lifetime)
    {
        Position = this.center = center; this.radius = radius; this.phase = phase;
        this.spin = spin; this.tint = tint; Lifetime = lifetime;
        glyph = Main.rand.Next(12);
    }
    public override void Update()
    {
        Rotation = phase + Time * spin;
        Position = center + Rotation.ToRotationVector2() * radius * (1f + .07f * LifetimeRatio);
        Color = tint * (MathF.Sin(MathHelper.Pi * LifetimeRatio) * .72f);
    }
    public override void Draw()
    {
        Texture2D texture = MostimaMaterials.Get("ChronoSigilV2");
        Vector2 symbol = (new Vector2(.5f) + (glyph * MathHelper.TwoPi / 12f).ToRotationVector2() * .34f) * texture.Size();
        int side = (int)(texture.Width * .12f);
        Rectangle source = new((int)symbol.X - side / 2, (int)symbol.Y - side / 2, side, side);
        Main.spriteBatch.Draw(texture, Position - Main.screenPosition, source, Color, Rotation + MathHelper.PiOver2,
            new Vector2(side * .5f), 25f / side, SpriteEffects.None, 0f);
    }
}
