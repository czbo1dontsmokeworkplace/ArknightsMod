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

namespace ArknightsMod.Content.Items.Armor.Vanguard.Texas
{
	[AutoloadEquip(EquipType.Body)]
	public class TexasBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 98;

		public override void SetArmorDefaults() {
			Item.defense = 26;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<TexasBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<ManganeseTrihydrate>(3)
			.AddIngredient<IntegratedDevice>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
