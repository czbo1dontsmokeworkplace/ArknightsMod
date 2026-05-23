using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Items.Material.T5;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Oblivionis
{
	[AutoloadEquip(EquipType.Head)]
	public class OblivionisHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override int ArmorLifeBonus => 206;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
	}
}
