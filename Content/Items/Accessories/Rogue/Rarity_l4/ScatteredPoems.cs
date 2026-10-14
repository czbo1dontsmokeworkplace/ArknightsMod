using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4
{
    public class ScatteredPoems : ModItem
    {
        

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
			Item.value = Item.sellPrice(0, 16, 0, 0);
			Item.rare = ItemRarityID.Master;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 增加召唤栏（方舟原版为不消耗部署栏）
            player.maxMinions += 3;
        }
    }
}