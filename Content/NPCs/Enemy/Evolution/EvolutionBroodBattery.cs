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
        // 一生只锁定一次。方向已包含在原有 ExtraAI 同步中，晚加入客户端无需重新瞄准。
        if (FlightDirection == Vector2.Zero)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) { NPC.velocity *= .9f; return; }
            FlightDirection = boss.FormationDirection(NPC.Center, NPC.ai[2] < 0 ? 0 : 1, 2);
            NPC.netUpdate = true;
        }
        float distance = NPC.Distance(player.Center);
        Vector2 drift = distance > 420 ? FlightDirection * 1.8f : Vector2.Zero;
        NPC.velocity = Vector2.Lerp(NPC.velocity, drift, .07f);
        NPC.rotation = FlightDirection.ToRotation();
        // 六连发，然后休息；每发都有渐入/渐出的独立预瞄，枪口可移动但射角永不追踪。
        int beat = (Age - 60) % 174;
        if (beat >= 20 && beat <= 110 && (beat - 20) % 18 == 0)
            boss.Shoot(EvolutionShot.Beam, NPC.Center, FlightDirection, 1750, 28, 58, beamStyle: EvolutionBeamStyle.Pulse);
    }
}
