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

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Skadi
{
	[AutoloadEquip(EquipType.Body)]
	public class SkadiBody : ArknightsVanityBody
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override void Load() {

		}
		public override void UpdateEquip(Player player)
		{
		}
	}
}