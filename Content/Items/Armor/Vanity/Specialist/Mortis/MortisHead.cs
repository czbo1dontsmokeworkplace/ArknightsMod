using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist.Mortis;

[AutoloadEquip(EquipType.Head)]
public class MortisHead : ArknightsVanityHead
{
	public override int Rarity => 5;
	public override void Load() {
	}
	public override void UpdateEquip(Player player) {
	}
	public override bool IsArmorSet(Item head, Item body, Item legs) {
		return body.type == ModContent.ItemType<MortisBody>() && legs.type == ModContent.ItemType<MortisLegs>();
	}
}
