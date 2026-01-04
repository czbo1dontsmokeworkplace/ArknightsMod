
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class ArmorMelanthaBody : ArknightsArmorBody
	{
		public override int Rarity => 3;
		public override int LifeBonus => 70;
		public override void SetArmorDefaults() {
			Item.defense = 13;
		}
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MelanthaBody>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
