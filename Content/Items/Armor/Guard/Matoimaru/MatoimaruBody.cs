using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Matoimaru
{
	[AutoloadEquip(EquipType.Body)]
	public class MatoimaruBody : NeoArmorBody
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 101;

		public override void SetArmorDefaults() {
			Item.defense = 8;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MatoimaruBody>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Polyester>(3)
			.AddIngredient<ManganeseOre>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
