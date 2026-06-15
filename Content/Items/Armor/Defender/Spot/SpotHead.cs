using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Defender.Spot
{
	[AutoloadEquip(EquipType.Head)]
	public class SpotHead : NeoArmorHead
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 184;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<SpotHead>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Diketon>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
