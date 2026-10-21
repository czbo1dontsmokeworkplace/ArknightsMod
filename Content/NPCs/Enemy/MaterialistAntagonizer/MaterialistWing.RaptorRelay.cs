using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private int raptorRelayAge = -1;
    private bool RaptorScanReady => raptorRelayAge < 0 ? raptorLegAge >= RaptorScanTicks :
        raptorRelayAge >= 84 && raptorLegAge >= 24;

    private void CommitRaptorScan(NPC npc, MaterialistAntagonizer boss)
    {
        if (boss.Phase == 3)
        {
            // Commit at the actual airframe position, never chase a moving readiness threshold.
            raptorRunStart = npc.Center;
            float dx = Main.player[boss.NPC.target].Center.X - npc.Center.X;
            if (System.Math.Abs(dx) > 1) raptorHeading = dx > 0 ? 1 : -1;
            aimAngle = RaptorDirection(RaptorEntryPitch).ToRotation();
        }
        SetRaptorLeg(npc, RaptorLeg.Scan);
    }

    private void PrepareRaptorRelief(NPC source, MaterialistAntagonizer boss)
    {
        if (!MaterialistAntagonizer.Authority) return;
        boss.RaptorNextSlot = Slot == 8 ? 9 : 8;
        boss.RaptorHandoffTicks = 120;
        boss.NPC.netUpdate = true;
        foreach (NPC escort in Main.ActiveNPCs)
        {
            if (escort.whoAmI == source.whoAmI || !escort.TryGetGlobalNPC<MaterialistWing>(out var wing) ||
                !wing.BelongsTo(boss) || wing.Retreating || Role(escort) != WingRole.Raptor) continue;
            // Sample the relief route once; player motion never resets its subsequent warning.
            wing.BeginRaptorSortie(escort, boss);
            wing.raptorRelayAge = 0;
            escort.netUpdate = true;
            break;
        }
    }
}
