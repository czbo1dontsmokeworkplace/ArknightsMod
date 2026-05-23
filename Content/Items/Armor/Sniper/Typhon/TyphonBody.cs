using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Typhon
{
	[AutoloadEquip(EquipType.Body)]
	public class TyphonBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 85;

		public override void SetArmorDefaults() {
			Item.defense = 8;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<TyphonBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<WhiteHorseKohl>(7)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
