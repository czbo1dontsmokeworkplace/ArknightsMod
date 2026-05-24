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

namespace ArknightsMod.Content.Items.Armor.Supporter.Deepcolor
{
	[AutoloadEquip(EquipType.Body)]
	public class DeepcolorBody : NeoArmorBody
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 52;

		public override void SetArmorDefaults() {
			Item.defense = 9;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<DeepcolorBody>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Polyketon>(2)
			.AddIngredient<OrironCluster>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
