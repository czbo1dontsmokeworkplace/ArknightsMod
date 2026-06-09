using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Melantha
{
	[AutoloadEquip(EquipType.Legs)]
	public class MelanthaLegs : NeoArmorLegs
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 70;

		public override void SetArmorDefaults() {
			Item.defense = 2;
		}
	}
}
