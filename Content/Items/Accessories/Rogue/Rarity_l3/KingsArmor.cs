using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Common.Items;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
    public class KingsArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Type] = true;

        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
			Item.value = Item.sellPrice(0, 6, 0, 0);
			Item.rare = ItemRarityID.Purple;
            Item.accessory = true;

            Item.GetGlobalItem<KingsGlobalItem>().isKingItem = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<KingsArmorPlayer>().active = true;
        }
    }

    public class KingsArmorPlayer : ModPlayer
    {
        public bool active;

        public override void ResetEffects() => active = false;

        public static int GetProgressionStacks()
        {
            int stacks = 1;
            if (NPC.downedMechBossAny) stacks++;
            if (NPC.downedPlantBoss) stacks++;
            if (NPC.downedGolemBoss) stacks++;
            if (NPC.downedAncientCultist) stacks++;
            if (NPC.downedMoonlord) stacks++;
            return stacks;
        }

        public override void PostUpdateEquips()
        {
            if (!active || Player.statLife >= Player.statLifeMax2 * 0.3f) return;
            int stacks = GetProgressionStacks();
            Player.statDefense += 5 * stacks;
            Player.endurance += 0.04f * stacks;
        }
    }
}
