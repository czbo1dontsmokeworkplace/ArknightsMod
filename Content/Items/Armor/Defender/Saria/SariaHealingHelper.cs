using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Saria;

// Called once by authoritative direct-healing settlements, never by regeneration ticks.
internal static class SariaHealingHelper
{
    internal static void OnDirectHeal(Player healer, Player patient, int actualAmount)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || actualAmount <= 0
            || healer == null || patient == null || !healer.active || healer.dead
            || !patient.active || patient.dead
            || !WearsSariaHelmet(healer)) return;

        if (Main.netMode == NetmodeID.SinglePlayer)
            patient.GetModPlayer<WeaponPlayer>().TryAddSP(2);
        else
        {
            // Weapon charge is maintained on the owning client. Only the server can
            // authorize a grant, and only the healed player's client receives it.
            ModPacket packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
            packet.Write((short)ArknightsMod.ArkMessageID.SariaHealingSPGrant);
            packet.Send(patient.whoAmI);
        }
    }

    private static bool WearsSariaHelmet(Player player) =>
        player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<SariaHead>();

    internal static void ReceiveGrant()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        Player patient = Main.LocalPlayer;
        if (patient.active && !patient.dead)
            patient.GetModPlayer<WeaponPlayer>().TryAddSP(2);
    }
}
