
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Utage;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai.Armor
{
	[AutoloadEquip(EquipType.Body)]
	internal class ArmorExusiaiBody:ArknightsArmorBody
	{
		public override int Rarity => 6;
		public override int LifeBonus => 84;
		public override void SetArmorDefaults() {
			Item.defense = 12;
		}
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<UtageBody>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
