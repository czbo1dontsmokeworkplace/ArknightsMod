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

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Saria.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ArmorSariaLegs : ArknightsArmorLegs
	{
		public override int LifeBonus => 158;
		public override void SetArmorDefaults() {
			Item.defense = 30;
		}
		public override void UpdateArmorEquip(Player Player) {

		}
	}

}