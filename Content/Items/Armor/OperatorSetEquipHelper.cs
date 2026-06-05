using Terraria;
using Terraria.Localization;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorSetEquipHelper
	{
		public static bool HasHelmet(Player player, int helmetItemType) {
			return player.armor[0].type == helmetItemType;
		}

		public static bool HasFullSet(Player player, int helmetItemType, int chestItemType, int greavesItemType) {
			return player.armor[0].type == helmetItemType
				&& player.armor[1].type == chestItemType
				&& player.armor[2].type == greavesItemType;
		}

		public static void ApplySetBonusText(Player player, bool fullSetActive, string setBonusKey) {
			player.setBonus = fullSetActive ? Language.GetTextValue(setBonusKey) : string.Empty;
		}
	}
}
