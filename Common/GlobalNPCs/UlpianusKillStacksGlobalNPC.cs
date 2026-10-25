using ArknightsMod.Content.Items.Armor.Guard.Ulpianus;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalNPCs;

public sealed class UlpianusKillStacksGlobalNPC : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || npc.friendly || npc.lifeMax <= 5
            || (uint)npc.lastInteraction >= Main.maxPlayers) return;
        Main.player[npc.lastInteraction].GetModPlayer<UlpianusSetPlayer>().AddKillStack();
    }
}
