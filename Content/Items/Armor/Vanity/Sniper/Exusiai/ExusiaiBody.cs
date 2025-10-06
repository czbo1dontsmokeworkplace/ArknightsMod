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

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai
{
    [AutoloadEquip(EquipType.Body)]
    public class ExusiaiBody : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            if (Main.netMode == NetmodeID.Server)
                return;
            ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
            ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true;
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
    } 
}
