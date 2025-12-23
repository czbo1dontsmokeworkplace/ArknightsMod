using ArknightsMod.Content.Items.Armor.Vanity;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Melanite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Provence
{
	[AutoloadEquip(EquipType.Head)]
	internal class ProvenceHead:ArknightsVanityHead
	{
		public override int Rarity => 5;
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ProvenceBody>() && legs.type == ModContent.ItemType<ProvenceLegs>();
		}
	}
}
