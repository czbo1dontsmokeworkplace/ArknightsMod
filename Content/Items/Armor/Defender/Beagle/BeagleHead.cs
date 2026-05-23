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
		public override int Value => 560000;
		public override int ArmorLifeBonus => 114;

		public override void SetArmorDefaults()
		{
			Item.defense = 0;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<BeagleBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<BeagleLegs>() && legs.neoarmor().hasUpgraded;
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
