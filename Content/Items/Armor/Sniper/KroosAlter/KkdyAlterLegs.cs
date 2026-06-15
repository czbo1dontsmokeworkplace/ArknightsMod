using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	[AutoloadEquip(EquipType.Legs)]
	public class KkdyAlterLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 62;
		
		public override void SetArmorDefaults() {
			Item.defense = 4;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KkdyAlterLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<OrironBlock>(3)
			.AddIngredient<SemiSyntheticSolvent>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
