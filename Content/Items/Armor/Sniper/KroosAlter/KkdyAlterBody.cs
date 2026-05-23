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

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	[AutoloadEquip(EquipType.Body)]
	public class KkdyAlterBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 76;

		public override void SetArmorDefaults() {
			Item.defense = 14;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<KkdyAlterBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<IncandescentAlloyBlock>(3)
			.AddIngredient<RMA7012>(3)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
