using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Follow the status carrier, so dashes cannot stretch a lingering particle field.
public sealed class T_Executor_Rune : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "ArknightsMod/Content/Textures/OperatorVfx/StarCore";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly Projectile carrier;
    private readonly int identity, owner, type, heldItemType;
    private readonly float phase, strength;

    internal T_Executor_Rune(Projectile carrier, float phase, Color tint, float strength)
    {
        this.carrier = carrier;
        identity = carrier.identity;
        owner = carrier.owner;
        type = carrier.type;
        heldItemType = Main.player[owner].HeldItem.type;
        this.phase = phase;
        this.strength = MathHelper.Clamp(strength, 0f, 1f);
        Position = carrier.Center;
        Color = tint;
        Lifetime = 24;
    }

    private bool Valid => carrier.active && carrier.identity == identity && carrier.owner == owner && carrier.type == type
        && Main.player[owner].active && !Main.player[owner].dead && Main.player[owner].HeldItem.type == heldItemType;

    public override void Update()
    {
        if (!Valid) { Kill(); return; }
        float angle = phase + Time * .035f;
        float gravity = Main.player[owner].gravDir;
        Position = carrier.Center + new Vector2(MathF.Cos(angle) * 25f,
            (MathF.Sin(angle) * 13f + 10f - Time * .55f) * gravity);
        Rotation = MathF.Sin(angle) * .2f;
    }

    public override void Draw()
    {
        if (!Valid) return;
        float fade = MathF.Sin(MathHelper.Pi * MathHelper.Clamp(LifetimeRatio, 0f, 1f)) * strength;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * (fade * .65f),
            Rotation, Texture.Size() * .5f, new Vector2(3f, 6f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
