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

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster.Mostima
{
    [AutoloadEquip(EquipType.Body)]
    public class MostimaBody : ArknightsVanityBody
    {
		public override int Rarity => 6;
	} 
}
