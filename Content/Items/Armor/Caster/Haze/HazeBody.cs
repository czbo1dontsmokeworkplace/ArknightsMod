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

namespace ArknightsMod.Content.Items.Armor.Caster.Haze
{
	[AutoloadEquip(EquipType.Body)]
	public class HazeBody : NeoArmorBody
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 71;

		public override void SetArmorDefaults() {
			Item.defense = 8;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<HazeBody>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Polyester>(3)
			.AddIngredient<OrironCluster>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
