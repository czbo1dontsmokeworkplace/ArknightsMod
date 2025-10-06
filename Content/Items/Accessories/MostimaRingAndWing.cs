using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Buffs;

namespace ArknightsMod.Content.Items.Accessories
{
    [AutoloadEquip(EquipType.Wings)]
    public class MostimaRingAndWing : ModItem
    {
        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.vanity = true;
            Item.width = Item.height = 40;
            Item.rare = ItemRarityID.Cyan;
            Item.value = 15000;
        }
        public override void UpdateEquip(Player player)
        {
            player.AddBuff(ModContent.BuffType<MostimaRingAndWingBuff>(), 1);
        }
        public override void UpdateVanity(Player player)
        {
            player.AddBuff(ModContent.BuffType<MostimaRingAndWingBuff>(), 1);
        }
    }
}
