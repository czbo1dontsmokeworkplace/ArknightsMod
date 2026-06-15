using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	[AutoloadEquip(EquipType.Head)]
	public class RaidianHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 137;

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
	}
}
