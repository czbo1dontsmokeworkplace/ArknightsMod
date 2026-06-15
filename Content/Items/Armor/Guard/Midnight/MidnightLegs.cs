using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Midnight
{
	[AutoloadEquip(EquipType.Legs)]
	public class MidnightLegs : NeoArmorLegs
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 83;
		
		public override void SetArmorDefaults() {
			Item.defense = 7;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MidnightLegs>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Polyketon>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
