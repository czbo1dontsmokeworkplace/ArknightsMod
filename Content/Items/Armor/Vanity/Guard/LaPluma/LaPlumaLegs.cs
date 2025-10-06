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
    [AutoloadEquip(EquipType.Legs)]
    public class LaPlumaLegs : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            if (Main.netMode == NetmodeID.Server)
                return;
            ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.value = 15000;
            Item.vanity = true;
        }
        public override void UpdateEquip(Player player)
        {
        }
    } 
}
