using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Typhon
{
	[AutoloadEquip(EquipType.Legs)]
	public class TyphonLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 85;
		
		public override void SetArmorDefaults() {
			Item.defense = 3;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<TyphonLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<CrystallineElectronicUnit>(6)
			.AddIngredient<CuttingFluidSolution>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
