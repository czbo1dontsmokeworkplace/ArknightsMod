using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	[AutoloadEquip(EquipType.Head)]
	public class KkdyAlterHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 125;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		


		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KkdyAlterHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<CrystallineCircuit>(7)
			.AddIngredient<OrironCluster>(10)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
