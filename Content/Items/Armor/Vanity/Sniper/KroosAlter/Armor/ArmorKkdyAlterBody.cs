using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria;
using Terraria.GameContent.Creative;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public class ArmorKkdyAlterBody : ArknightsArmorBody
	{
		public override int Rarity => 5;
		public override void SetArmorDefaults() {
			Item.defense = 23;
		}
		public override int LifeBonus => 62;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<KkdyAlterBody>(), 1)
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
