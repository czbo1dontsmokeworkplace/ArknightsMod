using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.W
{
	[AutoloadEquip(EquipType.Head)]
	public class WHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 161;

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
	}
}
