using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Surtr
{
	[AutoloadEquip(EquipType.Body)]
	public class SurtrBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 146;
		public override int Value => 560000;

		public override void Load() {
		}

		public override void SetArmorDefaults() {
			Item.defense = 31;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SurtrBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<BipolarNanoflake>(6)
			.AddIngredient<RMA7024>(5)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
