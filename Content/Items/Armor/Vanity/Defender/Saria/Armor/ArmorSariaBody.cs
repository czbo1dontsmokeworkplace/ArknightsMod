using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Saria.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class ArmorSariaBody : ArknightsArmorBody
	{
		public override int LifeBonus => 158;
		public override void SetArmorDefaults() {
			Item.defense = 89;
		}
		public override void UpdateArmorEquip(Player Player) {

		}
	}

}