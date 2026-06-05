using ArknightsMod.Content.Items.Armor.Caster.Amiya;
using ArknightsMod.Content.Items.Armor.Caster.Haze;
using ArknightsMod.Content.Items.Armor.Caster.Indigo;
using ArknightsMod.Content.Items.Armor.Caster.Lava;
using ArknightsMod.Content.Items.Armor.Caster.Mostima;
using ArknightsMod.Content.Items.Armor.Caster.Steward;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorCasterSetHelper
	{
		public static bool WearsFullCasterSet(Player player) {
			return HasSet(player, ModContent.ItemType<HazeHelmet>(), ModContent.ItemType<HazeChestplate>(), ModContent.ItemType<HazeGreaves>())
				|| HasSet(player, ModContent.ItemType<AmiyaHelmet>(), ModContent.ItemType<AmiyaChestplate>(), ModContent.ItemType<AmiyaGreaves>())
				|| HasSet(player, ModContent.ItemType<IndigoHelmet>(), ModContent.ItemType<IndigoChestplate>(), ModContent.ItemType<IndigoGreaves>())
				|| HasSet(player, ModContent.ItemType<LavaHelmet>(), ModContent.ItemType<LavaChestplate>(), ModContent.ItemType<LavaGreaves>())
				|| HasSet(player, ModContent.ItemType<MostimaHelmet>(), ModContent.ItemType<MostimaChestplate>(), ModContent.ItemType<MostimaGreaves>())
				|| HasSet(player, ModContent.ItemType<StewardHelmet>(), ModContent.ItemType<StewardChestplate>(), ModContent.ItemType<StewardGreaves>());
		}

		private static bool HasSet(Player player, int helmet, int chest, int greaves) {
			return OperatorSetEquipHelper.HasFullSet(player, helmet, chest, greaves);
		}
	}
}
