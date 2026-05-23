using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;

namespace ArknightsMod.Content.Items.Armor.Medic.Kaltsit.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorKaltsitLegs : ArknightsArmorLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Medic/Kaltsit/KaltsitLegs";
		public override int Rarity => 6;
		public override int LifeBonus => 82;
		public override void SetArmorDefaults() {
			Item.defense = 6;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KaltsitLegs>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<OptimizedDevice>(3)
			.AddIngredient<OrironCluster>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
