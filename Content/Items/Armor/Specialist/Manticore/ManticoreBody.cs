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
	[AutoloadEquip(EquipType.Body)]
	public class ManticoreBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 82;

		public override void SetArmorDefaults() {
			Item.defense = 28;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<ManticoreBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<OrironBlock>(3)
			.AddIngredient<SugarPack>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
