using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore
{
	[AutoloadEquip(EquipType.Legs)]
	public class ManticoreLegs : NeoArmorLegs
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 82;
		
		public override void SetArmorDefaults() {
			Item.defense = 9;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ManticoreLegs>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<KetonColloid>(3)
			.AddIngredient<PolyesterPack>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
