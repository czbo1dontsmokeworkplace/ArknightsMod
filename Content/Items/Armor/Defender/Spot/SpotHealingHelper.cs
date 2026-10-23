using System;
using System.IO;
using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Spot;

internal static class SpotHealingHelper
{
    private const double HealingMultiplier = 1.04d;

    internal static int MaximumAmount(int baseAmount) => baseAmount <= 0 ? 0
        : (int)Math.Min(int.MaxValue, Math.Floor(baseAmount * HealingMultiplier));

    internal static int ModifyOutgoingHeal(Player healer, int baseAmount)
    {
        if (baseAmount <= 0) return 0;
        // Inspect functional equipment directly; do not rely on frame-local flags.
        return healer != null && healer.active && !healer.dead
            && healer.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<SpotHead>()
            ? MaximumAmount(baseAmount) : baseAmount;
    }

    internal static void OnDirectHeal(Player healer, Player patient)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || healer.whoAmI == patient.whoAmI
            || !WearsSet(healer)) return;
        patient.AddBuff(ModContent.BuffType<SpotHealDodgeBuff>(), SpotHealDodgeBuff.DurationTicks, quiet: true);
        if (Main.netMode != NetmodeID.Server) return;
        // Buffs applied to a remote player must also reach their owning client,
        // which executes dodge rolls and supplies the local PRTS snapshot.
        var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
        packet.Write((short)ArknightsMod.ArkMessageID.SpotHealDodgeGrant);
        packet.Write((byte)patient.whoAmI);
        packet.Send();
    }

    private static bool WearsSet(Player healer) =>
        healer.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<SpotHead>()
        && healer.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<SpotBody>()
        && healer.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<SpotLegs>();

    internal static void ReceiveGrant(BinaryReader reader)
    {
        int index = reader.ReadByte();
        if (Main.netMode != NetmodeID.MultiplayerClient || index >= Main.maxPlayers) return;
        Player patient = Main.player[index];
        if (patient.active && !patient.dead)
            patient.AddBuff(ModContent.BuffType<SpotHealDodgeBuff>(), SpotHealDodgeBuff.DurationTicks, quiet: true);
    }
}
