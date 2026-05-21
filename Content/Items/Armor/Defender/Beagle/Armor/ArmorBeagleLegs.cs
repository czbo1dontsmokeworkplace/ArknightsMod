using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Beagle.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorBeagleLegs : ArknightsArmorLegs
    {
		public override string Texture => "ArknightsMod/Content/Items/Armor/Defender/Beagle/BeagleLegs";
		public override int LifeBonus => 76;
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
