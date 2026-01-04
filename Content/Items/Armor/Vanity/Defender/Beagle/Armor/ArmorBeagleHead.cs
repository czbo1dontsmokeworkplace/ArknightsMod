using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor
{
	[AutoloadEquip(EquipType.Head)]
    public class ArmorBeagleHead : ArknightsArmorHead
    {
		public override int LifeBonus => 114;
		public override void SetArmorDefaults()
		{
		}
		public override bool IsArmorSet(Item head, Item body, Item legs)
        {
			return body.type == ModContent.ItemType<ArmorBeagleBody>() &&
				legs.type == ModContent.ItemType<ArmorBeagleLegs>();
        }
        public override void UpdateArmorEquip(Player Player)
        {
			Player.GetModPlayer<ArknightsArmorPlayer>().extraDefenseBonus += 0.05f;
		}
        public override void UpdateArmorSet(Player player)
        {
			player.setBonus = "";
            player.GetModPlayer<BeagleSetPlayer>().BeagleSetActive = true;
        }
		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient(ModContent.ItemType<BeagleHead>(), 1)
			.AddIngredient(ModContent.ItemType<Orundum>(), 30)
			.AddTile(ModContent.TileType<FactoryTile>())
			.Register();
		}
	}
}
