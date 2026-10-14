using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Common.Items;
namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
    public class MonarchSidearm : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 20;
			Item.value = Item.sellPrice(0, 6, 0, 0);
			Item.rare = ItemRarityID.Purple;
            Item.accessory = true;

			Item.GetGlobalItem<KingsGlobalItem>().isKingItem = true;
		}

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<MonarchSidearmPlayer>().active = true;
        }
    }

    public class MonarchSidearmPlayer : ModPlayer
    {
        public bool active;

        public override void ResetEffects() => active = false;

        private bool IsActive => active && Player.statLife < Player.statLifeMax2 * 0.3f;

        public override float UseTimeMultiplier(Item item) => IsActive ? 0.8f : 1f;
        public override float UseAnimationMultiplier(Item item) => IsActive ? 0.8f : 1f;
    }
}
