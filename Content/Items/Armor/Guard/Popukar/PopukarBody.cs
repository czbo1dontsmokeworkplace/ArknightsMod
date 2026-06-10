using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Popukar
{
	[AutoloadEquip(EquipType.Body)]
	public class PopukarBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 93;

		public override void SetArmorDefaults() {
			Item.defense = 19;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<PopukarBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Oriron>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
