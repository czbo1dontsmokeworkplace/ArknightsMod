using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Supporter.Deepcolor
{
	// PRTS 官方数据（精二满级）：生命 1050 / 防御 125，由 OperatorArmorStatFormula 统一换算
	[AutoloadEquip(EquipType.Legs)]
	public class DeepcolorLegs : NeoArmorLegs
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => OperatorArmorStatFormula.LegsLifeBonus(1050);

		public override void SetArmorDefaults() {
			Item.defense = OperatorArmorStatFormula.LegsDefenseBonus(125);
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
