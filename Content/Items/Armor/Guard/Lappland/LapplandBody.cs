using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Lappland
{
	[AutoloadEquip(EquipType.Body)]
	public class LapplandBody : NeoArmorBody
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 118;

		public override void SetArmorDefaults() {
			Item.defense = 27;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<LapplandBody>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<OrirockConcentration>(3)
			.AddIngredient<Grindstone>(4)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
