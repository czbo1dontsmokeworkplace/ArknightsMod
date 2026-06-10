using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Melantha
{
	[AutoloadEquip(EquipType.Body)]
	public class MelanthaBody : NeoArmorBody
	{
		public override int Rarity => 3;
		public override int ArmorLifeBonus => 70;

		public override void SetArmorDefaults() {
			Item.defense = 6;
		}
	}
}
