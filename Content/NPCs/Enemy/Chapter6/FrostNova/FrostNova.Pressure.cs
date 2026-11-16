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
            Vector2 axis=(target-NPC.Center).SafeNormalize(Vector2.UnitY);
            FireFan(NPC.Center,axis,AirbornePhase?5:3,AirbornePhase?.16f:.21f,AirbornePhase?8.2f:7.2f,0,34);
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
