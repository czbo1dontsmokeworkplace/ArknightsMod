using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi
{
	[AutoloadEquip(EquipType.Legs)]
	public class SkadiLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override void Load() {
		}
	}
}