using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel
{
	[AutoloadEquip(EquipType.Legs)]
	public class AnselLegs : NeoArmorLegs
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 57;
		
		public override void SetArmorDefaults() {
			Item.defense = 3;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<AnselLegs>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Polyketon>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
