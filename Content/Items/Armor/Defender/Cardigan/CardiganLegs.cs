using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Defender.Cardigan
{
	[AutoloadEquip(EquipType.Legs)]
	public class CardiganLegs : NeoArmorLegs
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 122;
		
		public override void SetArmorDefaults() {
			Item.defense = 12;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<CardiganLegs>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Polyketon>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
