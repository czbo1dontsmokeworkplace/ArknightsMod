using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mortis
{
	[AutoloadEquip(EquipType.Legs)]
	public class MortisLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 111;
		
		public override void SetArmorDefaults() {
			Item.defense = 8;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MortisLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<CrystallineCircuit>(3)
			.AddIngredient<LoxicKohl>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
