using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy;

[AutoloadEquip(EquipType.Head)]
public class DorothyHead : NeoArmorHead
{
	public override int Rarity => 6;
	public override void Load() {
	}
	public override bool IsArmorSet(Item head, Item body, Item legs) {
		return body.type == ModContent.ItemType<DorothyBody>() && legs.type == ModContent.ItemType<DorothyLegs>();
	}
}
