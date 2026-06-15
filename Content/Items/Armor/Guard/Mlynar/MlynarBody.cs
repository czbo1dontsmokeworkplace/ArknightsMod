using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Mlynar
{
	[AutoloadEquip(EquipType.Body)]
	public class MlynarBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 213;

		public override void SetArmorDefaults() {
			Item.defense = 38;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MlynarBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<RMA7024>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
