using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle
{
	[AutoloadEquip(EquipType.Body)]
	public class BeagleBody : ArknightsArmorBody
    {
		public override (float ratio, int value) LifeReplacement => (0.25f, 76);
		public override void SetArmorDefaults()
		{
			Item.defense = 36;
		}
		public override void UpdateArmorEquip(Player Player)
		{

		}
    }
}
