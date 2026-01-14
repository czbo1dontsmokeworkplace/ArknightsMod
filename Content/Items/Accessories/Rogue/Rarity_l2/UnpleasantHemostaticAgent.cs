using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l2
{
    public class UnpleasantHemostaticAgent : ModItem
    {
       

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.value = Item.sellPrice(8, 0, 0, 0); // 15½ð±Ò¼ÛÖµ
            Item.rare = 1; 
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {

            player.statLifeMax2 += (int)(player.statLifeMax2 * 0.2f);
        }
    }
}