using ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter.Armor;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public class ArmorKkdyAlterLegs : ArknightsArmorLegs
    {
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
