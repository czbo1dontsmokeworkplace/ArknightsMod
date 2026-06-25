using ArknightsMod.Content.Items.Armor.Defender.Beagle;
using ArknightsMod.Content.Items.Armor.Defender.Cardigan;
using ArknightsMod.Content.Items.Armor.Defender.Mudrock;
using ArknightsMod.Content.Items.Armor.Defender.Nian;
using ArknightsMod.Content.Items.Armor.Defender.Saria;
using ArknightsMod.Content.Items.Armor.Defender.Spot;
using ArknightsMod.Content.Items.Armor.Defender.Vulcan;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorDefenderSetHelper
	{
		public static bool WearsFullDefenderSet(Player player) {
			return HasSet(player, ModContent.ItemType<BeagleHelmet>(), ModContent.ItemType<BeagleChestplate>(), ModContent.ItemType<BeagleGreaves>())
				|| HasSet(player, ModContent.ItemType<CardiganHelmet>(), ModContent.ItemType<CardiganChestplate>(), ModContent.ItemType<CardiganGreaves>())
				|| HasSet(player, ModContent.ItemType<SariaHelmet>(), ModContent.ItemType<SariaChestplate>(), ModContent.ItemType<SariaGreaves>())
				|| HasSet(player, ModContent.ItemType<NianHelmet>(), ModContent.ItemType<NianChestplate>(), ModContent.ItemType<NianGreaves>())
				|| HasSet(player, ModContent.ItemType<SpotHelmet>(), ModContent.ItemType<SpotChestplate>(), ModContent.ItemType<SpotGreaves>())
				|| HasSet(player, ModContent.ItemType<VulcanHelmet>(), ModContent.ItemType<VulcanChestplate>(), ModContent.ItemType<VulcanGreaves>())
				|| HasMudrockSet(player);
		}

		private static bool HasSet(Player player, int helmet, int chest, int greaves) {
			return OperatorSetEquipHelper.HasFullSet(player, helmet, chest, greaves);
		}

		private static bool HasMudrockSet(Player player) {
			return player.armor[0].type == ModContent.ItemType<MudrockHead>() && player.armor[0].neoarmor().hasUpgraded
				&& player.armor[1].type == ModContent.ItemType<MudrockBody>() && player.armor[1].neoarmor().hasUpgraded
				&& player.armor[2].type == ModContent.ItemType<MudrockLegs>() && player.armor[2].neoarmor().hasUpgraded;
		}
	}
}
