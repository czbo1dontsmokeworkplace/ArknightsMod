using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Texas
{
	[AutoloadEquip(EquipType.Legs)]
	public class TexasLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 98;
		
		public override void SetArmorDefaults() {
			Item.defense = 9;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<TexasLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<GrindstonePentahydrate>(3)
			.AddIngredient<LoxicKohl>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
