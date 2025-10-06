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
using Microsoft.Xna.Framework;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai
{
    [AutoloadEquip(EquipType.Head)]
    public class ExusiaiHead : ModItem
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
            Lighting.AddLight(player.Center, new Vector3(1f, 1f, 1f));
        }
        public override void UpdateVanity(Player player)
        {
            Lighting.AddLight(player.Center, new Vector3(1f, 1f, 1f));
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ExusiaiBody>() && legs.type == ModContent.ItemType<ExusiaiLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "No party no life";
        }
    } 
}
