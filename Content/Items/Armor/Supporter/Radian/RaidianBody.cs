using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	[AutoloadEquip(EquipType.Body)]
	public class RaidianBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 69;

		public override void SetArmorDefaults() {
			Item.defense = 12;
		}
	}
}
