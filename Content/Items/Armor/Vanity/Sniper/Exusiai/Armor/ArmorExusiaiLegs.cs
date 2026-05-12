using ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha;
using ArknightsMod.Content.Tiles.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	internal class ArmorExusiaiLegs:ArknightsArmorLegs
	{
		public override int Rarity => 6;
		public override int LifeBonus => 84;
		public override void SetArmorDefaults() {
			Item.defense = 4;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<MelanthaLegs>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}

}
