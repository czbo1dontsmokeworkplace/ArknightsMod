using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mortis;

[AutoloadEquip(EquipType.Head)]
public class MortisHead : NeoArmorHead
{
	public override int Rarity => 5;
	public override void Load() {
	}
	public override bool IsArmorSet(Item head, Item body, Item legs) {
		return body.type == ModContent.ItemType<MortisBody>() && legs.type == ModContent.ItemType<MortisLegs>();
	}
}
