using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle
{
	[AutoloadEquip(EquipType.Legs)]
	public class BeagleLegs : ArknightsArmorLegs
    {
		public override (float ratio, int value) LifeReplacement => (0.25f, 76);
		public override void SetArmorDefaults()
		{
			Item.defense = 12;
		}
		public override void UpdateArmorEquip(Player player)
        {
        }
    }
}
