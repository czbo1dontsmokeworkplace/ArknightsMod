using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Indigo
{
	[AutoloadEquip(EquipType.Head)]
	public class IndigoHead : NeoArmorHead
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 144;
		
		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		


		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<IndigoHead>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<Oriron>(1)
			.AddIngredient<RMA7012>(7)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
	
