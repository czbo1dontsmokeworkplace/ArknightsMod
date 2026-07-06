using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Supporter.Deepcolor
{
	// PRTS 官方数据（精二满级）：生命 1050 / 防御 125，由 OperatorArmorStatFormula 统一换算
	[AutoloadEquip(EquipType.Body)]
	public class DeepcolorBody : NeoArmorBody
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => OperatorArmorStatFormula.BodyLifeBonus(1050);

		public override void SetArmorDefaults() {
			Item.defense = OperatorArmorStatFormula.BodyDefenseBonus(125);
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<DeepcolorBody>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Polyketon>(2)
			.AddIngredient<OrironCluster>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
