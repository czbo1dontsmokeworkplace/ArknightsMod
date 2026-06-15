using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy
{
	[AutoloadEquip(EquipType.Legs)]
	public class DorothyLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 75;
		
		public override void SetArmorDefaults() {
			Item.defense = 4;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<DorothyLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<PolymerizedGel>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
