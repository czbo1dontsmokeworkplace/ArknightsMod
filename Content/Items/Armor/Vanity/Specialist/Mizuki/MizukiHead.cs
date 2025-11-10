using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria;
using Terraria.GameContent.Creative;
using ArknightsMod.Content.Items.Armor.Vanity;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist.Mizuki;

[AutoloadEquip(EquipType.Head)]
public class MizukiHead : ArknightsVanityHead
{
	public override int Rarity => 6;
	public override void Load() {
	}
	public override void UpdateEquip(Player player) {
	}
	public override bool IsArmorSet(Item head, Item body, Item legs) {
		return body.type == ModContent.ItemType<MizukiBody>() && legs.type == ModContent.ItemType<MizukiLegs>();
	}
}
