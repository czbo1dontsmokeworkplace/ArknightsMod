using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	[AutoloadEquip(EquipType.Legs)]
	public class RaidianLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 69;

		public override void SetArmorDefaults() {
			Item.defense = 4;
		}
	}
}
