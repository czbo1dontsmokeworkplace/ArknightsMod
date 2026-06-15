using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.Kaltsit
{
	[AutoloadEquip(EquipType.Legs)]
	public class KaltsitLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 102;
		
		public override void SetArmorDefaults() {
			Item.defense = 7;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KaltsitLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<CrystallineElectronicUnit>(6)
			.AddIngredient<GrindstonePentahydrate>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
