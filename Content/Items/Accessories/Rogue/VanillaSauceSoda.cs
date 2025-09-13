using ArknightsMod.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue
{
    public class VanillaSauceSoda : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.value = Item.sellPrice(8, 0, 0, 0); // 2金50银
            Item.accessory = true;
            Item.rare = 1;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 增加技力回复速度0.5/秒（加算）
			// ！！！未完成！！！
			// 不知道模组内的技力是哪个变量
            //player.GetModPlayer<WeaponPlayer>().skillRegenBonus += 0.2f / 60f;
        }
    }
}