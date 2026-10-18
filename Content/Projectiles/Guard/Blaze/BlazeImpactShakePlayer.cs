using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Blaze;

// Keep hit and activation shakes separate. Hit displacement is reduced by 60%;
// Specter originally used the full global amplitude, while Blaze/Gavial used half.
public sealed class BlazeImpactShakePlayer : ModPlayer
{
    private readonly ShakeMotion activation = new();
    private readonly ShakeMotion impact = new();
    private readonly ShakeMotion specterImpact = new();

    internal void Add(int duration, float strength) => activation.Add(duration, strength);

    // Scale the final displacement: changing only the initial impulse cannot reduce
    // a shake whose speed and maximum radius are normalized on every update.
    internal void AddImpact(int duration, float strength, bool specter = false)
        => (specter ? specterImpact : impact).Add(duration, strength);

    public override void ResetEffects()
    {
        activation.Tick();
        impact.Tick();
        specterImpact.Tick();
    }

    public override void PostUpdate()
    {
        activation.Update();
        impact.Update();
        specterImpact.Update();
    }

    public override void ModifyScreenPosition()
    {
        if (Player.whoAmI == Main.myPlayer)
            Main.screenPosition += activation.Offset * 0.5f
                + impact.Offset * (0.5f * 0.4f) + specterImpact.Offset * 0.4f;
    }

    public override void UpdateDead()
    {
        activation.Clear();
        impact.Clear();
        specterImpact.Clear();
    }

    private sealed class ShakeMotion
    {
        private int remaining;
        private Vector2 velocity;
        internal Vector2 Offset { get; private set; }

        internal void Add(int duration, float strength)
        {
            remaining = Math.Max(remaining, duration);
            Offset += Main.rand.NextVector2Circular(strength, strength);
            velocity = Main.rand.NextVector2Circular(1f, 1f).SafeNormalize(Vector2.UnitX) * Math.Max(2f, strength);
        }

        internal void Tick() { if (remaining > 0) remaining--; }

        internal void Update()
        {
            if (remaining > 0)
            {
                Offset += velocity;
                velocity = velocity.SafeNormalize(Vector2.Zero) * 4f;
                if (Offset.Length() >= 5f)
                {
                    Offset = Offset.SafeNormalize(Vector2.Zero) * 5f;
                    velocity = -4f * Offset.SafeNormalize(Vector2.Zero).RotatedByRandom(.5);
                }
            }
            else Offset = Offset.SafeNormalize(Vector2.Zero) * Math.Max(Offset.Length() - 10f, 0f);
        }

        internal void Clear()
        {
            remaining = 0;
            Offset = velocity = Vector2.Zero;
        }
    }
}
