using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Armor.Defender;
using Terraria;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorDefenderSetHelper
	{
		// 只认盔甲栏中的完整重装套装；纯时装与混搭不会触发队友光环。
		public static bool WearsFullDefenderSet(Player player) {
			return NeoArmorReforgeSetLoader.GetVanity(player.armor[0].type) is DefenderArmorHead head
				&& NeoArmorReforgeSetLoader.IsFullSetEquipped(player, head);
		}
	}
}
