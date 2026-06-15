using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	[AutoloadEquip(EquipType.Head)]
	internal class MudrockHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 443;

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
	}
}
