using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Common.Items;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4
{
    public class KingsCrown : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 设置king标签
            ItemID.Sets.ItemNoGravity[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
			Item.value = Item.sellPrice(0, 16, 0, 0);
			Item.rare = ItemRarityID.Purple;
            Item.accessory = true; 

     
            Item.GetGlobalItem<KingsGlobalItem>().isKingItem = true;
        }

        private int CountKingItems(Player player)
        {
            int count = 0;
            for (int i = 3; i < 8 + player.extraAccessorySlots && i < player.armor.Length; i++)
            {
                Item accessory = player.armor[i];
                if (!accessory.IsAir &&
                    accessory.type != Type && 
                    accessory.TryGetGlobalItem(out KingsGlobalItem kingItem) &&
                    kingItem.isKingItem)
                {
                    count++;
                }
            }
            return count;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<KingsCrownPlayer>().active = true;
        }

        internal int GetKingCount(Player player) => CountKingItems(player) + 1;
    }

    public class KingsCrownPlayer : ModPlayer
    {
        public bool active;

        public override void ResetEffects() => active = false;

        public override void PostUpdateEquips()
        {
            if (!active || Player.statLife >= Player.statLifeMax2 * 0.3f) return;
            int count = ModContent.GetInstance<KingsCrown>().GetKingCount(Player);
            Player.GetDamage(DamageClass.Generic) += count >= 3 ? 0.4f : 0.2f;
        }
    }
}
