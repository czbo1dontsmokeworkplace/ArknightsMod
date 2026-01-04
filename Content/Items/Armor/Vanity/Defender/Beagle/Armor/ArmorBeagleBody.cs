using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class ArmorBeagleBody : ArknightsArmorBody
    {
		public override int LifeBonus => 76;
		public override void SetArmorDefaults()
		{
			Item.defense = 18;
		}
		public override void UpdateArmorEquip(Player Player)
		{

		}
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<BeagleBody>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}

	}
}
