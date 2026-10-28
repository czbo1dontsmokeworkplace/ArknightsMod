using System;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorSniperSetHelper
	{
		// 狙击头部件没有共同的职业基类，按职业命名空间识别，再验证三件属于同一套。
		public static bool WearsFullSniperSet(Player player) {
			NeoArmorReforgeVanityItem head = NeoArmorReforgeSetLoader.GetVanity(player.armor[0].type);
			return head is NeoArmorReforgeVanityHead
				&& head.GetType().Namespace?.StartsWith("ArknightsMod.Content.Items.Armor.Sniper.", StringComparison.Ordinal) == true
				&& NeoArmorReforgeSetLoader.IsFullSetEquipped(player, head);
		}
	}
}
