using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.LaPluma
{
	[AutoloadEquip(EquipType.Body)]
	public class LaPlumaBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 113;

		public override void SetArmorDefaults() {
			Item.defense = 34;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<LaPlumaBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<KetonColloid>(3)
			.AddIngredient<CoagulatingGel>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
