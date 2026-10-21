using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Content.Projectiles.Guard.Reaper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Reaper_WispSpark : Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly ReaperKind kind;
    private readonly float curl, size;
    public T_Reaper_WispSpark(Vector2 position, Vector2 velocity, ReaperKind kind, int lifetime, float size)
    {
        Position = position; Velocity = velocity; this.kind = kind;
        Lifetime = lifetime; this.size = size; curl = Main.rand.NextFloat(-.055f, .055f);
    }
    public override void Update()
    {
        Velocity = (Velocity * .94f).RotatedBy(curl);
        Rotation = Velocity.ToRotation();
    }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - Math.Clamp(LifetimeRatio, 0, 1), 1.4f);
        Vector2 axis = Rotation.ToRotationVector2();
        float stretch = size * (1 + Velocity.Length() * .65f);
        ChargedOperatorVfx.Sprite("TrailFilament", Position - axis * stretch * .25f,
            new Vector2(stretch, size * .65f), ReaperRules.Color(kind) * (fade * .8f), Rotation);
        ChargedOperatorVfx.Sprite("StarCore", Position, new Vector2(size * 1.8f),
            ReaperRules.Edge(kind) * fade, Rotation);
    }
}

public sealed class T_Reaper_WispBurst : Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly ReaperKind kind;
    private readonly float angle;
    public T_Reaper_WispBurst(Vector2 position, ReaperKind kind, float angle)
    {
        Position = position; this.kind = kind; this.angle = angle; Lifetime = 16;
    }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1), fade = MathF.Pow(1 - t, 1.6f);
        ReaperWispMaterial.Impact(Position, kind, t, angle);
        float radius = MathHelper.Lerp(18, 86, 1 - (1 - t) * (1 - t));
        ChargedOperatorVfx.Sprite("OrbitSmear", Position, new Vector2(96, 62) * (1 + t * .4f),
            ReaperRules.Edge(kind) * (fade * .8f), angle);
        ChargedOperatorVfx.Sprite("RuptureRing", Position, new Vector2(radius, radius * .72f),
            ReaperRules.Color(kind) * (fade * .7f), angle);
        ChargedOperatorVfx.Sprite("ImpactPetals", Position, new Vector2(60 * (1 + t), 38),
            ReaperRules.Edge(kind) * (fade * .6f), angle);
        ChargedOperatorVfx.Sprite("StarCore", Position, new Vector2(40, 22) * (1 - t),
            ReaperRules.Edge(kind) * (fade * .9f), angle);
        float flash = Math.Max(0, 1 - Time / 7f);
        ChargedOperatorVfx.Sprite("StarCore", Position, new Vector2(84, 52), Color.White * flash, angle);
    }
}

public sealed class T_Reaper_WispVapor : Particle
{
    public override string TexturePath => "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    private readonly Vector2 size;
    public T_Reaper_WispVapor(Vector2 position, Vector2 velocity, ReaperKind kind)
    {
        Position = position; Velocity = velocity; Lifetime = 20;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        Color = Color.Lerp(new Color(22, 25, 32), ReaperRules.Color(kind), .24f);
        size = new Vector2(22, 16);
    }
    public override void Update() { Velocity *= .92f; Rotation += .025f; }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * ((1 - t) * .3f),
            Rotation, Texture.Size() * .5f, size * (1 + t) / Texture.Size(), SpriteEffects.None, 0);
    }
}
