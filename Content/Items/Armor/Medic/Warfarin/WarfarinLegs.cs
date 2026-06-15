using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
	[AutoloadEquip(EquipType.Legs)]
	public class WarfarinLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 76;
		
		public override void SetArmorDefaults() {
			Item.defense = 3;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<WarfarinLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<PolyesterLump>(3)
			.AddIngredient<OrirockCluster>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
