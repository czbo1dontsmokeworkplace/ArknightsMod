using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Supporter.Deepcolor
{
	// 数值范例：生命基数210 / 防御基数12（见 DeepcolorHead 注释），腿部取生命25%、防御25%。
	[AutoloadEquip(EquipType.Legs)]
	public class DeepcolorLegs : NeoArmorLegs
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 52; // 210 * 25%

		public override void SetArmorDefaults() {
			Item.defense = 3; // 12 * 25%
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<DeepcolorLegs>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Oriron>(2)
			.AddIngredient<Aketon>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
