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

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Utage
{
    [AutoloadEquip(EquipType.Head)]
    public class UtageHead : ModItem
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
            Item.rare = ItemRarityID.Cyan;
            Item.value = 15000;
            Item.vanity = true;
        }
        public override void Load()
        {
        }
        public override void UpdateEquip(Player player)
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<UtageBody>() && legs.type == ModContent.ItemType<UtageLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "超大杯（各种意义上的）";
        }
    } 
}
