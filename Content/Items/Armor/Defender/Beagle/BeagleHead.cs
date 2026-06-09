using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T1;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Beagle
{
	[AutoloadEquip(EquipType.Head)]
    public class BeagleHead : NeoArmorHead
    {
		public override int Rarity => 3;

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override int Value => 560000;
		public override int ArmorLifeBonus => 115;

		public override void SetArmorDefaults()
		{
			Item.defense = 0;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<BeagleHead>(1)
			.AddIngredient<Orundum>(30)
			.AddIngredient<Diketon>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}
	}
}
