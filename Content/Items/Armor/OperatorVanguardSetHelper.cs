using ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe;
using ArknightsMod.Content.Items.Armor.Vanguard.Fang;
using ArknightsMod.Content.Items.Armor.Vanguard.Plume;
using ArknightsMod.Content.Items.Armor.Vanguard.Texas;
using ArknightsMod.Content.Items.Armor.Vanguard.Vanilla;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorVanguardSetHelper
	{
		public static bool WearsFullVanguardSet(Player player) {
			return HasSet(player, ModContent.ItemType<BagpipeHead>(), ModContent.ItemType<BagpipeBody>(), ModContent.ItemType<BagpipeLegs>())
				|| HasSet(player, ModContent.ItemType<FangHead>(), ModContent.ItemType<FangBody>(), ModContent.ItemType<FangLegs>())
				|| HasSet(player, ModContent.ItemType<PlumeHead>(), ModContent.ItemType<PlumeBody>(), ModContent.ItemType<PlumeLegs>())
				|| HasSet(player, ModContent.ItemType<TexasHead>(), ModContent.ItemType<TexasBody>(), ModContent.ItemType<TexasLegs>())
				|| HasSet(player, ModContent.ItemType<VanillaHead>(), ModContent.ItemType<VanillaBody>(), ModContent.ItemType<VanillaLegs>());
		}

		private static bool HasSet(Player player, int helmet, int chest, int greaves) {
			return OperatorSetEquipHelper.HasFullSet(player, helmet, chest, greaves);
		}
	}
}
