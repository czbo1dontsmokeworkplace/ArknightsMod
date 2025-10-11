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
    [AutoloadEquip(EquipType.Head)]
    public class MostimaHead : ArknightsVanityHead
    {
		public override int Rarity => 6;
		public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MostimaBody>() && legs.type == ModContent.ItemType<MostimaLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "砸，瓦路多！";
        }
    } 
}
