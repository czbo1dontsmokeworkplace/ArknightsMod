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

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	[AutoloadEquip(EquipType.Legs)]
	public class UtageLegs : NeoArmorLegs
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 172;
		
		public override void SetArmorDefaults() {
			Item.defense = 8;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<UtageLegs>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<OrirockCube>(2)
			.AddIngredient<ManganeseOre>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
