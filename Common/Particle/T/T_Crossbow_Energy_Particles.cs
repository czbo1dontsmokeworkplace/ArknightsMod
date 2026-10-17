using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Particle.T;

// A bright, squashed energy grain, with a saturated halo and a small solid-looking core.
// It becomes round as it brakes; its curved drift belongs to the grain, not its emitter.
public sealed class T_Crossbow_Energy_Particle : Particle
{
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly Color tint, endColor;
    private readonly float diameter, curl, drag;
    private readonly bool glint;
    public T_Crossbow_Energy_Particle(Vector2 position, Vector2 velocity, Color tint, Color endColor,
        float diameter, int life, float curl, bool glint = false)
    {
        Position = position; Velocity = velocity; this.tint = tint; this.endColor = endColor;
        this.diameter = diameter; Lifetime = life; this.curl = curl; this.glint = glint;
        drag = Main.rand.NextFloat(.92f, .96f);
        Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity = Velocity.RotatedBy(curl * (1f - LifetimeRatio)) * drag;
        if (Velocity.LengthSquared() > .01f) Rotation = Velocity.ToRotation();
    }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = (1f - t * t) * MathHelper.Clamp((1f - t) * 4f, 0f, 1f);
        float stretch = MathHelper.Clamp(Velocity.Length() * .22f, 0f, 1.8f);
        float size = diameter * (1f - t * .65f);
        Vector2 shape = new Vector2(1f + stretch, 1f / (1f + stretch * .4f)) * size;
        Color color = Color.Lerp(tint, endColor, t * t);
        DrawMask(Texture, shape * 1.8f, color * (fade * .45f));
        DrawMask(Texture, shape, color * fade);
        DrawMask(Texture, shape * .4f, Color.Lerp(color, Color.White, .72f) * fade);
        if (glint)
        {
            Texture2D cross = ModContent.Request<Texture2D>(
                "ArknightsMod/Content/Projectiles/Sniper/Typhon/Effects/Cross").Value;
            float pulse = .65f + .35f * MathF.Sin(t * MathHelper.Pi);
            DrawMask(cross, new Vector2(size * 1.8f, size * 1.1f), color * (fade * pulse * .7f));
        }
    }
    private void DrawMask(Texture2D texture, Vector2 size, Color color)
        => Main.spriteBatch.Draw(texture, Position - Main.screenPosition, null, color,
            Rotation, texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);
}

// Two independently timed masks create a pressure front followed by a breaking rim.
// This is a textured particle, with no collision, damage, screen flash or camera movement.
public sealed class T_Crossbow_Shock_Particle : Particle
{
    public override string TexturePath => "ArknightsMod/Content/Textures/ChargedSniper/BreachRing";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly Color tint;
    private readonly Vector2 dimensions;
    private readonly bool broken;
    private readonly float opacity;
    private readonly int delay;
    public T_Crossbow_Shock_Particle(Vector2 position, Color tint, Vector2 dimensions,
        float rotation, int life, int delay, bool broken, float opacity)
    {
        Position = position; this.tint = tint; this.dimensions = dimensions; Rotation = rotation;
        Lifetime = life + delay; this.delay = delay; this.broken = broken; this.opacity = opacity;
    }
    public override void Draw()
    {
        if (Time < delay) return;
        float t = MathHelper.Clamp((Time - delay) / (float)(Lifetime - delay), 0f, 1f);
        float expand = 1f - MathF.Pow(1f - t, 3f);
        float fade = MathF.Pow(1f - t, 1.45f) * opacity;
        Texture2D texture = broken ? ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Textures/OperatorVfx/RuptureRing").Value : Texture;
        Vector2 size = dimensions * (.16f + .84f * expand);
        float rotation = Rotation + (broken ? t * .5f : 0f);
        Color color = Color.Lerp(Color.Lerp(tint, Color.White, .32f), tint, expand);
        Main.spriteBatch.Draw(texture, Position - Main.screenPosition, null, color * fade, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);
    }
}

// A fast luminous explosion body, separate from the slower pressure rim and drifting debris.
public sealed class T_Crossbow_Burst_Particle : Particle
{
    public override string TexturePath => "ArknightsMod/Content/Textures/OperatorVfx/CrystalVortex";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly Color tint, edge;
    private readonly Vector2 dimensions;
    private readonly float spin;
    public T_Crossbow_Burst_Particle(Vector2 position, Color tint, Color edge,
        Vector2 dimensions, float rotation, int life)
    {
        Position = position; this.tint = tint; this.edge = edge; this.dimensions = dimensions;
        Rotation = rotation; Lifetime = life; spin = Main.rand.NextBool() ? .045f : -.045f;
    }
    public override void Update() => Rotation += spin;
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float expand = 1f - MathF.Pow(1f - t, 2.5f);
        float fade = MathF.Pow(1f - t, 1.6f);
        Vector2 size = dimensions * (.35f + expand * .65f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint * (fade * .7f),
            Rotation, Texture.Size() * .5f, size / Texture.Size(), SpriteEffects.None, 0f);
        Texture2D glow = ModContent.Request<Texture2D>("ArknightsMod/Common/Particle/DefaultParticle").Value;
        Main.spriteBatch.Draw(glow, Position - Main.screenPosition, null,
            Color.Lerp(edge, Color.White, .5f) * (MathF.Pow(1f - t, 3f) * .85f),
            Rotation, glow.Size() * .5f, size * .6f / glow.Size(), SpriteEffects.None, 0f);
    }
}
