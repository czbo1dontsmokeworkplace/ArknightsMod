using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Oblivionis
{
	[AutoloadEquip(EquipType.Legs)]
	public class OblivionisLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 118;
		
		public override void SetArmorDefaults() {
			Item.defense = 11;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<OblivionisLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<NucleicCrystalSinter>(6)
			.AddIngredient<PolymerizedGel>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
