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

namespace ArknightsMod.Content.Items.Armor.Guard.Mlynar
{
	[AutoloadEquip(EquipType.Body)]
	public class MlynarBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 195;

		public override void SetArmorDefaults() {
			Item.defense = 38;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MlynarBody>(1)
			.AddIngredient<Orundum>(60)
			.AddIngredient<RMA7024>(3)
			.AddIngredient<ManganeseOre>(9)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
