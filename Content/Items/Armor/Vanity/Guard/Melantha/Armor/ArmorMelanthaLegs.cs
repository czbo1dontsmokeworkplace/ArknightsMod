using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorMelanthaLegs : ArknightsArmorLegs
	{
		public override int Rarity => 3;
		public override int LifeBonus => 70;
		public override void SetArmorDefaults() {
			Item.defense = 4;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MelanthaLegs>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
