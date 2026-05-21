using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Defender.Saria.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorSariaLegs : ArknightsArmorLegs
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Defender/Saria/SariaLegs";
		public override int LifeBonus => 158;
		public override void SetArmorDefaults() {
			Item.defense = 30;
		}
		public override void UpdateArmorEquip(Player Player) {

		}
	}

}