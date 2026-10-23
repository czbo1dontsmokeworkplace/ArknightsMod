using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.NPCs.Friendly;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalNPCs;

// Shared Charger branch trait: one cost per defeated enemy, never per thrust,
// spark, echo or segment. Server ownership prevents client/server duplication.
public sealed class ChargerDeploymentCost : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || npc.friendly || npc.lifeMax <= 5 || npc.SpawnedFromStatue
            || npc.realLife >= 0 || (uint)npc.lastInteraction >= Main.maxPlayers) return;
        Player killer = Main.player[npc.lastInteraction];
        if (!killer.active || killer.dead || killer.HeldItem.ModItem is not VanguardSpearWeapon) return;
        DeploymentCost.SpawnOrb(npc.GetSource_Death(), npc.Center, 1);
    }
}
