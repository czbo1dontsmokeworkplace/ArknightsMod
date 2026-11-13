using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    // Cryogen's moving radial salvos / delayed bombs, using FrostNova's own
    // crystals and finite warnings. No extra attack IDs: movement remains 20%.
    private void AddCryogenicPressure(Player player, int pattern)
    {
        int phase = AirbornePhase ? 2 : 1;
        if (!FrostNovaCombatRules.PressureBeat(phase, pattern, combatTimer)) return;
        Vector2 target = player.Center + player.velocity * 10;
        float gap = (target - NPC.Center).SafeNormalize(Vector2.UnitY).ToRotation();
        bool radial = (attackSerial + combatTimer / 96) % 2 == 0;
        FrostNovaVisuals.Cast(NPC.Center, radial ? 0 : 1, .85f);
        if (radial) {
            // 12/16 beams is the Cryogen tier; the near-player opening is fixed
            // at emission, so the attack rewards moving through the formation.
            int count = AirbornePhase ? 16 : 12;
            float opening = FrostNovaCombatRules.RingGap(phase) + .12f;
            for (int i = 0; i < count; i++) {
                float angle = gap + (i + .5f) * MathHelper.TwoPi / count;
                if (FrostNovaCombatRules.InGap(angle, gap, opening)) continue;
                Vector2 axis = angle.ToRotationVector2();
                FireShot(NPC.Center + axis * 42, axis * (AirbornePhase ? 7.2f : 6.4f), 0, 30);
            }
        }
        else {
            Vector2 aim = (target - NPC.Center).SafeNormalize(Vector2.UnitY);
            // Keep the old +/-0.46 outer rays, removing the two interior rays.
            FireFan(NPC.Center, aim, 3, AirbornePhase ? .46f : .23f, AirbornePhase ? 8.2f : 7.2f, 1, 30);
            // Existing mine, staggered away from the player's current location.
            if (AirbornePhase && pattern is not (7 or 8)) {
                Vector2 at = target + new Vector2(attackSerial % 2 == 0 ? -300 : 300, -140);
                FireShot(at, (target - at).SafeNormalize(Vector2.UnitY), 2, 72);
            }
        }
    }
}
