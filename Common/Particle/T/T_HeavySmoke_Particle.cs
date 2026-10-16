using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Projectiles.Sniper.Fiammetta;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Supporter.Magallan;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_HeavySmoke_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => MagallanVisuals.SmokePath;
    private readonly float diameter;
    private readonly float opacity;
    private readonly float lift;
    private readonly float spin;
    private readonly int variant;
    private readonly Color tint;

    public T_HeavySmoke_Particle(Vector2 position, Vector2 velocity, Color tint, float diameter,
        int lifetime, float opacity, float lift)
    {
        Position = position;
        Velocity = velocity;
        this.tint = tint;
        this.diameter = MathHelper.Clamp(diameter, 8f, 120f);
        this.opacity = MathHelper.Clamp(opacity, 0f, 0.65f);
        this.lift = lift;
        Lifetime = Math.Clamp(lifetime, 12, 60);
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        spin = Main.rand.NextFloat(-0.017f, 0.017f);
        variant = Main.rand.Next(1, 5);
    }

    public override void Update()
    {
        Velocity *= 0.92f;
        Velocity.Y += lift;
        Rotation += spin;
    }

    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = MathHelper.Clamp(t * 6f, 0f, 1f) * MathF.Pow(1f - t, 1.25f) * opacity;
        float size = diameter * (0.65f + 0.65f * MathF.Sin(t * MathHelper.Pi * 0.8f));
        Texture2D shape = ModContent.Request<Texture2D>($"ArknightsMod/Assets/GrayScaleTexture/Smoke{variant}").Value;
        Vector2 scale = new Vector2(size, size * (0.76f + 0.08f * MathF.Sin(t * 4f))) / shape.Size();
        Color shadow = Color.Lerp(tint, new Color(42, 60, 72), 0.48f);
        Main.EntitySpriteDraw(shape, Position - Main.screenPosition, null, shadow * fade,
            Rotation, shape.Size() * 0.5f, scale, SpriteEffects.None);
        Main.EntitySpriteDraw(Texture, Position - Main.screenPosition + new Vector2(-size * 0.05f, -size * 0.08f),
            null, tint * (fade * 0.7f), -Rotation * 0.65f,
            Texture.Size() * 0.5f, new Vector2(size * 0.86f, size * 0.68f) / Texture.Size(), SpriteEffects.None);
    }
}
