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

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.LaPluma
{
    [AutoloadEquip(EquipType.Body)]
    public class LaPlumaBody : ArknightsVanityBody
    {
		public override int Rarity => 5;
	} 
}
