using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaTemporalShard : Particle
{
    private readonly Color tint;
    private readonly float size;
    private readonly float spin;
    public override string TexturePath => MostimaVisuals.Assets + "TemporalStar";
    public override BlendState DrawBlendState => BlendState.Additive;
    public MostimaTemporalShard(Vector2 position, Vector2 velocity, Color tint, int lifetime, float size)
    {
        Position = position; Velocity = velocity; this.tint = Color = tint;
        Lifetime = lifetime; this.size = size; spin = Main.rand.NextFloat(-.07f, .07f);
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update()
    {
        Velocity *= .955f;
        Rotation += spin;
        Color = tint * MathF.Sin(MathHelper.Pi * MathHelper.Clamp(Time / (float)Lifetime, 0f, 1f));
    }
    public override void Draw()
    {
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, new Vector2(size, size * .55f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}

public sealed class MostimaOrbitGlyph : Particle
{
    private readonly Vector2 center;
    private readonly float radius;
    private readonly float phase;
    private readonly float spin;
    private readonly Color tint;
    private readonly int glyph;
    public override string TexturePath => MostimaVisuals.Assets + "ChronoSigilV2";
    public override BlendState DrawBlendState => BlendState.Additive;
    public MostimaOrbitGlyph(Vector2 center, float radius, float phase, float spin, Color tint, int lifetime)
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
