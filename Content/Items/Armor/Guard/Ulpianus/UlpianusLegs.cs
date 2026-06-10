using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Ulpianus
{
	[AutoloadEquip(EquipType.Legs)]
	public class UlpianusLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 326;
		
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<UlpianusLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<SolidifiedFiberBoard>(6)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
