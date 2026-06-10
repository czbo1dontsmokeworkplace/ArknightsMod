using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Sniper.Adnachiel
{
	[AutoloadEquip(EquipType.Body)]
	public class AdnachielBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 54;

		public override void SetArmorDefaults() {
			Item.defense = 10;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<AdnachielBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Polyester>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
