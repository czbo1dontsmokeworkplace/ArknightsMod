using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	[AutoloadEquip(EquipType.Body)]
	internal class MudrockBody : NeoArmorBody
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 222;

		public override void SetArmorDefaults() {
			Item.defense = 50;
		}
	}
}
