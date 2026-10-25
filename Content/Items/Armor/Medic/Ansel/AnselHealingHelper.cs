using System;
using System.IO;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel;

internal static class AnselHealingHelper
{
    internal static void OnDirectHeal(Player healer, Player primaryTarget, int actualAmount)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || actualAmount <= 0
            || !MedicHealingNet.Allied(healer, primaryTarget) || !WearsSet(healer)
            || Main.rand.NextFloat() >= .07f) return;

        Player chosen = null;
        int count = 0;
        foreach (Player candidate in Main.ActivePlayers)
        {
            if (candidate.whoAmI == primaryTarget.whoAmI || !MedicHealingNet.Allied(healer, candidate)
                || candidate.statLife >= candidate.statLifeMax2
                || candidate.GetModPlayer<IncomingHealingPlayer>().HealingBlocked) continue;
            if (Main.rand.Next(++count) == 0) chosen = candidate;
        }
        if (chosen == null) return;

        // Copy the primary target's final HP gain, without multiplying bonuses again.
        int amount = Math.Min(actualAmount, chosen.statLifeMax2 - chosen.statLife);
        if (Main.netMode == NetmodeID.SinglePlayer)
            chosen.GetModPlayer<IncomingHealingPlayer>().HealFinal(amount);
        else
        {
            chosen.statLife += amount;
            var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
            packet.Write((short)ArknightsMod.ArkMessageID.AnselChainHealingGrant);
            packet.Write((byte)chosen.whoAmI);
            packet.Write(amount);
            packet.Send(chosen.whoAmI);
        }
        // The extra heal is a direct heal, but cannot roll another chain heal.
        DirectHealingEffects.OnDirectHeal(healer, chosen, amount, allowChainHeal: false);
    }

    private static bool WearsSet(Player player) =>
        player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<AnselHead>()
        && player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<AnselBody>()
        && player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<AnselLegs>();

    internal static void ReceiveGrant(BinaryReader reader)
    {
        int index = reader.ReadByte(), amount = reader.ReadInt32();
        if (Main.netMode != NetmodeID.MultiplayerClient || index != Main.myPlayer || amount <= 0) return;
        Player player = Main.LocalPlayer;
        if (!player.active || player.dead) return;
        player.GetModPlayer<IncomingHealingPlayer>().HealFinal(Math.Min(amount, Math.Max(0, player.statLifeMax2 - player.statLife)));
        NetMessage.SendData(MessageID.PlayerLifeMana, number: index);
    }
}
