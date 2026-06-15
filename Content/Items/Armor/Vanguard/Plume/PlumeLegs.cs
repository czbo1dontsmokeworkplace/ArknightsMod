using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Plume
{
	[AutoloadEquip(EquipType.Legs)]
	public class PlumeLegs : NeoArmorLegs
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 61;
		
		public override void SetArmorDefaults() {
			Item.defense = 7;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<PlumeLegs>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<OrirockCube>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
