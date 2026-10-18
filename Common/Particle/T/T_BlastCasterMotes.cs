using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Particle.T;

// Short-lived material grains. All sizes below are world-space pixels.
public sealed class T_BlastCasterEmber : Particle
{
    internal static Texture2D Glow;
    public override string TexturePath => "ArknightsMod/Content/Textures/OperatorVfx/ForwardFlame";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly Color tint;
    private readonly float size, curl;
    private readonly bool optical;
    internal T_BlastCasterEmber(Vector2 at, Vector2 velocity, Color tint, float size, int life, bool optical)
    {
        Position = at; Velocity = velocity; this.tint = tint; this.size = size; Lifetime = life;
        this.optical = optical; Rotation = velocity.ToRotation(); curl = Main.rand.NextFloat(-.07f, .07f);
    }
    public override void Update()
    {
        Velocity = Velocity.RotatedBy(curl * (1 - LifetimeRatio)) * .91f;
        Velocity.Y -= optical ? .015f : .055f;
        Rotation = Velocity.ToRotation();
    }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        float fade = MathF.Sin(MathHelper.Pi * MathF.Pow(t, .6f)) * (1 - t);
        Vector2 shape = optical ? new Vector2(2.6f - t * 1.5f, .6f + t * .25f) : new Vector2(1.35f + t, .7f + t * .5f);
        Color color = Color.Lerp(tint, optical ? new Color(42, 110, 153) : new Color(124, 39, 12), t * t);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, color * (fade * (optical ? .65f : .78f)),
            Rotation, Texture.Size() * .5f, shape * (size * (1 - t * .35f)) / Texture.Size(), SpriteEffects.None, 0);
        Texture2D glow = Glow ??= ModContent.Request<Texture2D>("ArknightsMod/Common/Particle/DefaultParticle").Value;
        Main.spriteBatch.Draw(glow, Position - Main.screenPosition, null, Color.Lerp(tint, Color.White, .35f) * (fade * (optical ? .45f : .52f)),
            Rotation, glow.Size() * .5f, new Vector2(size * .7f, size * .5f) / glow.Size(), SpriteEffects.None, 0);
    }
}

public sealed class T_BlastCasterSoot : Particle
{
    public override string TexturePath => "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    public override BlendState DrawBlendState => BlendState.NonPremultiplied;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly float size, spin;
    internal T_BlastCasterSoot(Vector2 at, Vector2 velocity, float size)
    {
        Position = at; Velocity = velocity; this.size = size; Lifetime = 24;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi); spin = Main.rand.NextFloat(-.025f, .025f);
    }
    public override void Update() { Velocity *= .94f; Velocity.Y -= .035f; Rotation += spin; }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        float alpha = MathF.Sin(t * MathHelper.Pi) * (1 - t) * .22f;
        // Straight-alpha smoke: no A=0 pseudo-additive trick.
        Color color = new Color(42, 34, 30) * alpha;
        color.R = 42; color.G = 34; color.B = 30;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, color, Rotation,
            Texture.Size() * .5f, new Vector2(size * (1 + t), size * (.7f + t)) / Texture.Size(), SpriteEffects.None, 0);
    }
}
