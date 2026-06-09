using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.W
{
	[AutoloadEquip(EquipType.Legs)]
	public class WLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 80;

		public override void SetArmorDefaults() {
			Item.defense = 3;
		}
	}
}
