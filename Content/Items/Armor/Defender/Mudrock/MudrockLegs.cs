using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	[AutoloadEquip(EquipType.Legs)]
	internal class MudrockLegs : NeoArmorLegs
	{
		public override int Rarity => 6;
		public override int ArmorLifeBonus => 222;

		public override void SetArmorDefaults() {
			Item.defense = 17;
		}

		public override string Texture => "ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockGreaves";
	}
}
