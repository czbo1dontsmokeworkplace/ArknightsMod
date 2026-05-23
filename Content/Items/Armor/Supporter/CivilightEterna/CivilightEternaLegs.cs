using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna
{
	[AutoloadEquip(EquipType.Legs)]
	public class CivilightEternaLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 82;
		
		public override void Load() {
		}

		public override void SetArmorDefaults() {
			Item.defense = 6;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<CivilightEternaLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<CrystallineElectronicUnit>(6)
			.AddIngredient<OrironBlock>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
