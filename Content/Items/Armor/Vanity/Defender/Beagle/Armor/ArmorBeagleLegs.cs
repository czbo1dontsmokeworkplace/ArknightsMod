using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorBeagleLegs : ArknightsArmorLegs
    {
		public override (float ratio, int value) LifeReplacement => (0.25f, 76);
		public override void SetArmorDefaults()
		{
			Item.defense = 6;
		}
		public override void UpdateArmorEquip(Player player)
        {
        }
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<BeagleLegs>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
