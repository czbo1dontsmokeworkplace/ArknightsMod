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

namespace ArknightsMod.Content.Items.Armor.Caster.Indigo
{
	[AutoloadEquip(EquipType.Body)]
	public class IndigoBody : NeoArmorBody
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 72;

		public override void SetArmorDefaults() {
			Item.defense = 9;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<IndigoBody>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<OrirockCube>(4)
			.AddIngredient<ManganeseOre>(2)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
