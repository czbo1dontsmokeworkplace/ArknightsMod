using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.LaPluma
{
	[AutoloadEquip(EquipType.Head)]
	public class LaPlumaHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 225;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<LaPlumaHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<KetonColloid>(7)
			.AddIngredient<ManganeseOre>(13)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
