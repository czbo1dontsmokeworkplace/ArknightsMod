using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public class ArmorKkdyAlterHead : ArknightsArmorHead
	{
		public override int Rarity => 5;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override (float ratio, int value) LifeReplacement => (0.5f, 125);
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<KkdyAlterHead>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 50)

				.AddIngredient(ModContent.ItemType<CrystallineCircuit>(), 3)
				.AddIngredient(ModContent.ItemType<OrironCluster>(), 4)
				.AddIngredient(ModContent.ItemType<Polyester>(), 1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
		

		public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ArmorKkdyAlterBody>() && legs.type == ModContent.ItemType<ArmorKkdyAlterLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
			player.GetModPlayer<KkdyAlterSetPlayer>().KkdyAlterSetActive = true;
			player.setBonus = "";
        }
    } 
}
