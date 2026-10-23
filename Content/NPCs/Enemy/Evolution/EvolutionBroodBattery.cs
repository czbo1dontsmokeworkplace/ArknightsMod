using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public abstract partial class EvolutionBroodNPC
{
    private void DoFixedBeamBattery(Evolution boss, Player player)
    {
        if (boss.Transitioning || boss.Attack != 1 || Age > 360)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) { NPC.ai[3] = -35; NPC.netUpdate = true; }
            return;
        }
        // 先锁定初始方向；后续仅在发射时限角追转。方向通过原有 ExtraAI 同步。
        if (FlightDirection == Vector2.Zero)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) { NPC.velocity *= .9f; return; }
            FlightDirection = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
            NPC.netUpdate = true;
        }
        float distance = NPC.Distance(player.Center);
        Vector2 drift = distance > 420 ? FlightDirection * 1.8f : Vector2.Zero;
        NPC.velocity = Vector2.Lerp(NPC.velocity, drift, .07f);
        NPC.rotation = FlightDirection.ToRotation();
        // Each authoritative pulse turns by at most five degrees; crossed-clock checks cannot skip enraged pulses.
        if (EvolutionQuadrantRules.BatteryFire(Age, boss.CycleBoost) && Main.netMode != NetmodeID.MultiplayerClient)
        {
            FlightDirection = EvolutionQuadrantRules.TurnBattery(FlightDirection, NPC.Center, player.Center);
            NPC.netUpdate = true;
            boss.Shoot(EvolutionShot.Beam, NPC.Center, FlightDirection, 1750, 28, 58, beamStyle: EvolutionBeamStyle.Pulse);
        }
    }
}
