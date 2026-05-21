using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public class ArmorKkdyAlterLegs : ArknightsArmorLegs
    {
		public override string Texture => "ArknightsMod/Content/Items/Armor/Sniper/KroosAlter/KkdyAlterLegs";
		public override int Rarity => 5;
		public override void SetArmorDefaults() {
			Item.defense = 8;
		}
		public override int LifeBonus => 62;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<KkdyAlterLegs>(), 1)
				.AddIngredient(ModContent.ItemType<Orundum>(), 50)

				.AddIngredient(ModContent.ItemType<CrystallineCircuit>(), 2)
				.AddIngredient(ModContent.ItemType<OrironCluster>(), 3)
				.AddIngredient(ModContent.ItemType<Polyester>(), 2)
				.AddIngredient(ModContent.ItemType<Polyketon>(), 1)
				.AddTile(ModContent.TileType<FactoryTile>())
				.Register();
		}
		
    } 
}
