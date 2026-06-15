using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Supporter.Orchid
{
	[AutoloadEquip(EquipType.Legs)]
	public class OrchidLegs : NeoArmorLegs
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 47;
		
		public override void SetArmorDefaults() {
			Item.defense = 2;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<OrchidLegs>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Sugar>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
