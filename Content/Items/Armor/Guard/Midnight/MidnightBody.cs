using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Guard.Midnight
{
	[AutoloadEquip(EquipType.Body)]
	public class MidnightBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 83;

		public override void SetArmorDefaults() {
			Item.defense = 21;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MidnightBody>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Device>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
