using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi
{
	[AutoloadEquip(EquipType.Head)]
	public class SkadiHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 387;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		


		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SkadiHead>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<D32Steel>(6)
			.AddIngredient<OrirockConcentration>(7)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
