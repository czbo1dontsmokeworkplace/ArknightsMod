using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Harmonie;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Music_Note_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly int variant;
    private readonly Color tint;
    private readonly float tilt;
    public override string TexturePath => HarmonieVisuals.NotePath(0);
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Music_Note_Particle(Vector2 position, Vector2 velocity, int variant, Color color)
    {
        this.variant = variant;
        tint = color;
        Position = position;
        Velocity = velocity;
        Lifetime = 28;
        tilt = Main.rand.NextFloat(-.2f, .2f);
    }
    public override void Update()
    {
        Velocity *= .94f;
        Velocity.Y -= .015f;
    }
    public override void Draw()
    {
        float fade = MathF.Sin(MathHelper.Clamp(LifetimeRatio, 0f, 1f) * MathHelper.Pi);
        // 粒子管理器每个类型只缓存一张图；四种音符在绘制时从已有资源缓存选择。
        Texture2D note = ModContent.Request<Texture2D>(HarmonieVisuals.NotePath(variant)).Value;
        Main.spriteBatch.Draw(note, Position - Main.screenPosition, null, tint * (fade * .65f),
            tilt + MathF.Sin(LifetimeRatio * 4f) * .1f, note.Size() * .5f,
            8f / note.Height, SpriteEffects.None, 0f);
    }
}
