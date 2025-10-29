using ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle.Armor
{
	[AutoloadEquip(EquipType.Head)]
    public class ArmorBeagleHead : ArknightsArmorHead
    {
		public override (float ratio, int value) LifeReplacement => (0.5f, 114);
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
    }
}
