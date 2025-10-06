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

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter
{
    [AutoloadEquip(EquipType.Head)]
    public class KkdyAlterHead : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            if (Main.netMode == NetmodeID.Server)
                return;
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
            ArmorIDs.Head.Sets.IsTallHat[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightPurple;
            Item.value = 15000;
            Item.vanity = true;
        }
        public override void UpdateEquip(Player player)
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<KkdyAlterBody>() && legs.type == ModContent.ItemType<KkdyAlterLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "单手扫射M4，大家叫我克洛丝！";
        }
    } 
}
