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
			return HasSet(player, ModContent.ItemType<HazeHead>(), ModContent.ItemType<HazeBody>(), ModContent.ItemType<HazeLegs>())
				|| HasSet(player, ModContent.ItemType<AmiyaHead>(), ModContent.ItemType<AmiyaBody>(), ModContent.ItemType<AmiyaLegs>())
				|| HasSet(player, ModContent.ItemType<IndigoHead>(), ModContent.ItemType<IndigoBody>(), ModContent.ItemType<IndigoLegs>())
				|| HasSet(player, ModContent.ItemType<LavaHead>(), ModContent.ItemType<LavaBody>(), ModContent.ItemType<LavaLegs>())
				|| HasSet(player, ModContent.ItemType<MostimaHead>(), ModContent.ItemType<MostimaBody>(), ModContent.ItemType<MostimaLegs>())
				|| HasSet(player, ModContent.ItemType<StewardHead>(), ModContent.ItemType<StewardBody>(), ModContent.ItemType<StewardLegs>());
		}

		private static bool HasSet(Player player, int helmet, int chest, int greaves) {
			return OperatorSetEquipHelper.HasFullSet(player, helmet, chest, greaves);
		}
	}
}
