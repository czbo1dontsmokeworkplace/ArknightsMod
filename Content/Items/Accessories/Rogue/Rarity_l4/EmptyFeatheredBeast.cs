using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4
{
    public class EmptyFeatheredBeast : ModItem
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
            player.maxMinions += 1;
            player.whipRangeMultiplier += 0.12f;
            player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.12f;
        }
    }
}
