using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Defender.Beagle
{
	[AutoloadEquip(EquipType.Legs)]
	public class BeagleLegs : NeoArmorLegs
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 57;
		public override void SetArmorDefaults() {
			Item.defense = 6;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<BeagleLegs>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Device>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
