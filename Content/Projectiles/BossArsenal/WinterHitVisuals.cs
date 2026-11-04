using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using ArknightsMod.Content.Projectiles.Charged;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

internal static class WinterHitVisuals
{
    internal static void Emit(Vector2 at, Vector2 travel)
    {
        if (Main.dedServ) return;
        Vector2 axis = travel.SafeNormalize(Vector2.UnitX);
        SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = .6f, Pitch = .25f, MaxInstances = 4 }, at);
        // LightingBolts: pooled vanilla PrettySparkle particles, driven through the
        // target in the attack direction, with no stationary rotating ring.
        for (int i = 0; i < 14; i++) {
            Vector2 direction = axis.RotatedBy(Main.rand.NextFloat(-.42f, .42f));
            ChargedOperatorVfx.Sparkle(at + axis * Main.rand.NextFloat(0, 16), direction * Main.rand.NextFloat(4, 12),
                i % 3 == 0 ? Color.White : Color.LightCyan, axis.ToRotation(), Main.rand.NextFloat(.45f, .9f));
        }
        for (int i = 0; i < 7; i++) {
            var dust = Dust.NewDustPerfect(at + axis * Main.rand.NextFloat(0, 18), DustID.Snow,
                axis.RotatedBy(Main.rand.NextFloat(-.3f, .3f)) * Main.rand.NextFloat(4, 9), 70, Color.LightCyan, 1);
            dust.noGravity = true;
        }
    }
}
