using ArknightsMod.Common.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue
{
    public class KingsStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Type] = true;

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.value = Item.sellPrice(16, 0, 0, 0);
            Item.rare = ItemRarityID.Purple;
            Item.accessory = true;

            Item.GetGlobalItem<KingsGlobalItem>().isKingItem = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅在生命值低于30%时生效
            if ((float)player.statLife / player.statLifeMax2 <= 0.3f)
            {
                // 魔力回复（每秒10点，不满时才生效）
                if (player.statMana < player.statManaMax2 &&
                    player.GetModPlayer<CrownedAmplifierPlayer>().manaTimer++ >= 60)
                {
                    player.statMana += 10;
                    player.GetModPlayer<CrownedAmplifierPlayer>().manaTimer = 0;
                }

                // 生命恢复（每秒1.5%最大生命）
                player.GetModPlayer<CrownedAmplifierPlayer>().lifeRegenBonus =
                    (int)(player.statLifeMax2 * 0.015f);
            }
            else
            {
                // 生命值高于30%时重置效果
                player.GetModPlayer<CrownedAmplifierPlayer>().manaTimer = 0;
                player.GetModPlayer<CrownedAmplifierPlayer>().lifeRegenBonus = 0;
            }
        }
    }

    public class CrownedAmplifierPlayer : ModPlayer
    {
        public int manaTimer;
        public int lifeRegenBonus;

        public override void ResetEffects()
        {
            manaTimer = 0;
            lifeRegenBonus = 0;
        }

        public override void UpdateLifeRegen()
        {
            if (lifeRegenBonus > 0)
            {
                Player.lifeRegen += lifeRegenBonus;
            }
        }
    }
}