using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorSkadiLegs : ArknightsArmorLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Guard/Skadi/SkadiLegs";
		public override int Rarity => 6;
		public override int LifeBonus => 193;
		public override void SetArmorDefaults() {
			Item.defense = 6;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SkadiLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<ManganeseTrihydrate>(4)
			.AddIngredient<IntegratedDevice>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
