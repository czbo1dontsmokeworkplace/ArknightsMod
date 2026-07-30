using ArknightsMod.Content.Items.Armor.Reforge;
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
			return HasSet(player, ModContent.ItemType<BeagleHead>(), ModContent.ItemType<BeagleBody>(), ModContent.ItemType<BeagleLegs>())
				|| HasSet(player, ModContent.ItemType<CardiganHead>(), ModContent.ItemType<CardiganBody>(), ModContent.ItemType<CardiganLegs>())
				|| HasSet(player, ModContent.ItemType<SariaHead>(), ModContent.ItemType<SariaBody>(), ModContent.ItemType<SariaLegs>())
				|| HasSet(player, ModContent.ItemType<NianHead>(), ModContent.ItemType<NianBody>(), ModContent.ItemType<NianLegs>())
				|| HasSet(player, ModContent.ItemType<SpotHead>(), ModContent.ItemType<SpotBody>(), ModContent.ItemType<SpotLegs>())
				|| HasSet(player, ModContent.ItemType<VulcanHead>(), ModContent.ItemType<VulcanBody>(), ModContent.ItemType<VulcanLegs>())
				|| HasMudrockSet(player);
		}

		private static bool HasSet(Player player, int helmet, int chest, int greaves) {
			return OperatorSetEquipHelper.HasFullSet(player, helmet, chest, greaves);
		}

		// NeoArmor Reforge：泥岩迁到新系统后，套装件是独立 ItemID，不再需要 hasUpgraded 判断。
		private static bool HasMudrockSet(Player player) {
			return player.armor[0].type == ReforgeSetLoader.GetSetType<MudrockHead>()
				&& player.armor[1].type == ReforgeSetLoader.GetSetType<MudrockBody>()
				&& player.armor[2].type == ReforgeSetLoader.GetSetType<MudrockLegs>();
		}
	}
}
