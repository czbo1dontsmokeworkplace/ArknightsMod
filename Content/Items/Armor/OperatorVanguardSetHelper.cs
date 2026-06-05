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
			return HasSet(player, ModContent.ItemType<BagpipeHelmet>(), ModContent.ItemType<BagpipeChestplate>(), ModContent.ItemType<BagpipeGreaves>())
				|| HasSet(player, ModContent.ItemType<FangHelmet>(), ModContent.ItemType<FangChestplate>(), ModContent.ItemType<FangGreaves>())
				|| HasSet(player, ModContent.ItemType<PlumeHelmet>(), ModContent.ItemType<PlumeChestplate>(), ModContent.ItemType<PlumeGreaves>())
				|| HasSet(player, ModContent.ItemType<TexasHelmet>(), ModContent.ItemType<TexasChestplate>(), ModContent.ItemType<TexasGreaves>())
				|| HasSet(player, ModContent.ItemType<VanillaHelmet>(), ModContent.ItemType<VanillaChestplate>(), ModContent.ItemType<VanillaGreaves>());
		}

		private static bool HasSet(Player player, int helmet, int chest, int greaves) {
			return OperatorSetEquipHelper.HasFullSet(player, helmet, chest, greaves);
		}
	}
}
