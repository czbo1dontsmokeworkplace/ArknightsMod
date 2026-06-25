namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	/// <summary> 泥岩套装“手持右键切换形态”共用逻辑。装备栏内右键默认会被原版用于卸下装备，无法用来做形态切换，故改为手持物品右键触发。</summary>
	internal static class MudrockToggle
	{
		public static void Toggle(NeoArmorItem armorItem) {
			var neoArmor = armorItem.Item.neoarmor();
			neoArmor.hasUpgraded = !neoArmor.hasUpgraded;
			armorItem.RefreshState();
		}
	}
}
