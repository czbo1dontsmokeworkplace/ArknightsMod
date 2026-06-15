using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	[AutoloadEquip(EquipType.Body)]
	public class UtageBody : NeoArmorBody
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 98;

		public override void SetArmorDefaults() {
			Item.defense = 16;
		}
	}
}
