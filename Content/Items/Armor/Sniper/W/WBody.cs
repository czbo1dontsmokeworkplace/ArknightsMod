using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.W
{
	[AutoloadEquip(EquipType.Body)]
	public class WBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 80;

		public override void SetArmorDefaults() {
			Item.defense = 10;
		}
	}
}
