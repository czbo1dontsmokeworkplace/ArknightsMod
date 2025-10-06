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
using ArknightsMod.Common.Items;

namespace ArknightsMod.Content.Items.Accessories.Rogue
{
    public class KingsCrown : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 添加king标签
            ItemID.Sets.ItemNoGravity[Item.type] = true;

            // 明确告诉游戏这不是可装备物品

        }
       
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.value = Item.sellPrice(16, 0, 0, 0);
            Item.rare = ItemRarityID.Purple;
            Item.accessory = true; // 这是关键，保持为饰品但不装备

            // 添加自定义标签
            Item.GetGlobalItem<KingsGlobalItem>().isKingItem = true;
        }
        private int CountKingItems(Player player)
        {
            int count = 0;
            for (int i = 3; i < 8; i++) // 仅检测普通饰品栏
            {
                Item accessory = player.armor[i];
                if (!accessory.IsAir &&
                    accessory.type != Type && // 排除自身避免重复计数
                    accessory.TryGetGlobalItem(out KingsGlobalItem kingItem) &&
                    kingItem.isKingItem)
                {
                    count++;
                }
            }
            return count; // 不再+1
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if ((float)player.statLife / player.statLifeMax2 <= 0.3f)
            {
                int kingCount = CountKingItems(player) + 1; // 在此处+1
                float damageBonus = kingCount >= 3 ? 1.5f : kingCount * 0.5f;
                player.GetDamage(DamageClass.Generic) += damageBonus;
            }
        }

        
    }
}