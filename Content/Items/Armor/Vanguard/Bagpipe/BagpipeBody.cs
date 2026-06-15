using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Bagpipe
{
	[AutoloadEquip(EquipType.Body)]
	public class BagpipeBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 124;

		public override void SetArmorDefaults() {
			Item.defense = 29;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<BagpipeBody>(1)
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
