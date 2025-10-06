using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue
{
    public class ManifestationPendant : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 28;
            Item.value = Item.sellPrice(8, 0, 0, 0); // 5金币价值
            Item.rare = ItemRarityID.Pink; // 紫色稀有度
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += 0.15f; // 增加25%远程伤害
        }
    }
}