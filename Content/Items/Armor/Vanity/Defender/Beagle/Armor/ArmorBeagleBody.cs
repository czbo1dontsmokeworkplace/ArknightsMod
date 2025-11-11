using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class ArmorBeagleBody : ArknightsArmorBody
    {
		public override (float ratio, int value) LifeReplacement => (0.25f, 76);
		public override void SetArmorDefaults()
		{
			Item.defense = 36;
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
