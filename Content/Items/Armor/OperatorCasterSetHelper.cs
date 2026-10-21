using ArknightsMod.Content.Items.Armor.Caster;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorCasterSetHelper
	{
		// 只认盔甲栏中的套装件；纯时装查不到对应套装，不会触发。
		public static bool WearsFullCasterSet(Player player) {
			return NeoArmorReforgeSetLoader.GetVanity(player.armor[0].type) is CasterArmorHead head
				&& NeoArmorReforgeSetLoader.IsFullSetEquipped(player, head);
		}
	}
}
