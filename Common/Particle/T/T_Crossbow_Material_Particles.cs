using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Particle.T;

// These particles leave their emitter immediately. Their shape follows their own velocity,
// never the current position or rotation of an arrow that may already have hit something.
public sealed class T_Crossbow_Spark_Particle : Particle
{
    public override string TexturePath => "ArknightsMod/Content/Projectiles/Guard/Mousse/Assets/MousseLightSplinter";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly Color tint;
    private readonly float length, thickness, gravity, initialSpeed;
    public T_Crossbow_Spark_Particle(Vector2 position, Vector2 velocity, Color color,
        float length, float thickness, int life, float gravity = 0f)
    {
        Position = position; Velocity = velocity; tint = color;
        this.length = length; this.thickness = thickness; this.gravity = gravity;
        initialSpeed = Math.Max(.1f, velocity.Length()); Lifetime = life;
        Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity *= .88f;
        Velocity.Y += gravity;
        if (Velocity.LengthSquared() > .01f) Rotation = Velocity.ToRotation();
    }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = MathF.Pow(1f - t, 1.6f);
        float stretch = .28f + .72f * MathHelper.Clamp(Velocity.Length() / initialSpeed, 0f, 1f);
        Vector2 size = new(length * stretch, thickness * (1f - t * .85f));
        Color color = Color.Lerp(tint, new Color(255, 246, 225), (1f - t) * .22f) * fade;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, color, Rotation,
            Texture.Size() * .5f, size / Texture.Size(), SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color.White * (fade * .35f), Rotation,
            Texture.Size() * .5f, size * new Vector2(.6f, .32f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}

public sealed class T_Crossbow_Mote_Particle : Particle
{
    public enum Material { Fleck, Frost, Venom, Ink }
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => material == Material.Ink ? BlendState.AlphaBlend : BlendState.Additive;
    private readonly Material material;
    private readonly Color tint;
    private readonly float diameter, spin;
    public T_Crossbow_Mote_Particle(Vector2 position, Vector2 velocity, Color color,
        float size, int life, Material material)
    {
        Position = position; Velocity = velocity; tint = color; diameter = size;
        Lifetime = life; this.material = material;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        spin = Main.rand.NextFloat(-.12f, .12f);
    }
    public override void Update()
    {
        float t = LifetimeRatio;
        switch (material)
        {
            case Material.Venom:
                Velocity *= .9f; Velocity.Y += .055f;
                Rotation = Velocity.ToRotation();
                break;
            case Material.Frost:
                Velocity *= .95f; Velocity.X += MathF.Sin(Time * .24f + Rotation) * .025f;
                Velocity.Y += .018f; Rotation += spin;
                break;
            case Material.Ink:
                Velocity *= .91f; Velocity.Y += .025f;
                Rotation += spin * (1f - t);
                break;
            default:
                Velocity *= .94f; Velocity.Y += .065f; Rotation += spin;
                break;
        }
    }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = MathF.Pow(1f - t, material == Material.Venom ? 1.1f : 1.6f);
        Vector2 size = material switch {
            Material.Venom => new(diameter * (1f + Math.Min(1.6f, Velocity.Length() * .3f)), diameter * .7f),
            Material.Ink => new(diameter * 1.6f, diameter * .65f),
            Material.Frost => new(diameter * .55f, diameter * 1.5f),
            _ => new(diameter, diameter * .5f) };
        size *= 1f - t * .55f;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint * fade,
            Rotation, Texture.Size() * .5f, size / Texture.Size(), SpriteEffects.None, 0f);
        if (material != Material.Ink)
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null,
                Color.Lerp(tint, Color.White, .7f) * (fade * .65f), Rotation,
                Texture.Size() * .5f, size * .38f / Texture.Size(), SpriteEffects.None, 0f);
    }
}

public sealed class T_Crossbow_Wisp_Particle : Particle
{
    public override string TexturePath => "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    private readonly Color tint;
    private readonly float diameter, spin, opacity;
    private readonly int variant;
    public T_Crossbow_Wisp_Particle(Vector2 position, Vector2 velocity, Color color,
        float size, int life, float opacity = .3f)
    {
        Position = position; Velocity = velocity; tint = color; diameter = size;
        Lifetime = life; this.opacity = opacity;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        spin = Main.rand.NextFloat(-.035f, .035f);
        variant = Main.rand.Next(1, 5);
    }
    public override void Update()
    {
        Velocity *= .9f; Velocity.Y -= .018f; Rotation += spin;
    }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        // A delayed, soft body remains after the tiny bright droplets have extinguished.
        float fade = MathHelper.Clamp(t * 7f, 0f, 1f) * MathF.Pow(1f - t, 1.3f) * opacity;
        Texture2D texture = ModContent.Request<Texture2D>(
            "ArknightsMod/Assets/GrayScaleTexture/Smoke" + variant).Value;
        Vector2 size = new Vector2(diameter, diameter * .72f) * (.7f + t * .9f);
        Main.spriteBatch.Draw(texture, Position - Main.screenPosition, null,
            Color.Lerp(tint, new Color(24, 30, 39), t * .45f) * fade, Rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);
    }
}
