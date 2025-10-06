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

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Melanite
{
    [AutoloadEquip(EquipType.Legs)]
    public class MelaniteLegs : ArknightsVanityLegs
	{
		public override int Rarity => ItemRarityID.LightPurple;
		public override void UpdateEquip(Player player)
        {
        }
    } 
}
