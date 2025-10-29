using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Terraria.UI.Chat;
using System.Linq;
using System.Collections.Generic;
using Terraria.GameContent;


namespace ArknightsMod.Content.Items.Accessories.Rogue
{
    public class CoinOperatedToy : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.value = Item.sellPrice(16, 0, 0, 0);
            Item.rare = ItemRarityID.Master;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 计算玩家拥有的总金币数(以金币为单位)
            int totalGoldCoins = CountPlayerGold(player);

            // 每5金币增加3%攻速
            float attackSpeedBonus = (totalGoldCoins / 500) * 0.03f;
            player.GetAttackSpeed(DamageClass.Generic) += attackSpeedBonus;
        }

        private int CountPlayerGold(Player player)
        {
            int totalGold = 0;

            // 计算背包中的金币
            totalGold += CountContainerGold(player.inventory);

            // 计算存钱罐中的金币
            if (player.bank.item != null)
            {
                totalGold += CountContainerGold(player.bank.item);
            }

            return totalGold;
        }

        private int CountContainerGold(Item[] container)
        {
            int gold = 0;

            foreach (Item item in container)
            {
                switch (item.type)
                {
                    case ItemID.CopperCoin:
                        gold += item.stack / 10000; // 100铜=1银, 100银=1金 → 10000铜=1金
                        break;
                    case ItemID.SilverCoin:
                        gold += item.stack / 100;   // 100银=1金
                        break;
                    case ItemID.GoldCoin:
                        gold += item.stack;        // 1金=1金
                        break;
                    case ItemID.PlatinumCoin:
                        gold += item.stack * 100;  // 1铂金=100金
                        break;
                }
            }

            return gold;
        }
    }
}