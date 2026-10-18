using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// Keep the original hit-shake motion in its own channel so its reduced amplitude
// applies to every frame without weakening another weapon's concurrent shake.
public sealed class MushaHitShakePlayer : ModPlayer
{
    private int remaining;
    private Vector2 offset;
    private Vector2 velocity;
    private const float Strength = .3f;

    internal void Play(Vector2 direction, bool empowered)
    {
        remaining = Math.Max(remaining, empowered ? 4 : 2);
        velocity = direction.SafeNormalize(Vector2.UnitX) * (empowered ? 2.2f : 1.25f);
    }

    public override void ResetEffects()
    {
        if (remaining > 0) remaining--;
    }

    public override void PostUpdate()
    {
        if (remaining > 0)
        {
            offset += velocity;
            velocity = velocity.SafeNormalize(Vector2.Zero) * 4f;
            if (offset.LengthSquared() >= 25f)
            {
                offset = offset.SafeNormalize(Vector2.Zero) * 5f;
                velocity = -4f * offset.SafeNormalize(Vector2.Zero).RotatedByRandom(.5);
            }
        }
        else offset = Vector2.Zero;
    }

    public override void ModifyScreenPosition()
    {
        if (Player.whoAmI == Main.myPlayer) Main.screenPosition += offset * Strength;
    }

    public override void UpdateDead()
    {
        remaining = 0;
        offset = velocity = Vector2.Zero;
    }
}
