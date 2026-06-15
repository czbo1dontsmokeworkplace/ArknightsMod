using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Texas
{
	[AutoloadEquip(EquipType.Head)]
	public class TexasHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 195;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		


		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<TexasHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<PolyesterLump>(8)
			.AddIngredient<OrirockCluster>(16)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
