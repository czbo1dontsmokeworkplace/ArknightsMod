using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
    public class FallenSovereignForm : ModItem
    {

        

        public override void SetStaticDefaults()
        {
            
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(12, 0, 0, 0);
            Item.hasVanityEffects = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            //未完成，还没想好怎么做
           //放在这里只是为了更好适配其他国王套
        }

        
    }
}