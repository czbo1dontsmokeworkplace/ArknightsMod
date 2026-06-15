using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	[AutoloadEquip(EquipType.Legs)]
	public class RosmontisLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 97;
		
		public override void Load() {
		}

		public override void SetArmorDefaults() {
			Item.defense = 7;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<RosmontisLegs>(1)
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
