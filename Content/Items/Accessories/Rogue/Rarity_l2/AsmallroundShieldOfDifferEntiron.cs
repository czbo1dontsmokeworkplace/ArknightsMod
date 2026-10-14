using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l2
{
    public class AsmallroundShieldOfDifferEntiron : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
			Item.value = Item.sellPrice(0, 3, 0, 0);
			Item.rare = 1; // 橙色稀有度
            Item.accessory = true;
            Item.defense = 3;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SmallRoundShieldPlayer>().active = true;
        }
    }

    public class SmallRoundShieldPlayer : ModPlayer
    {
        public bool active;

        public override void ResetEffects() => active = false;

        public override void PostUpdate()
        {
            if (!active) return;
            // Buffs already lose one tick normally; one extra tick halves their duration.
            for (int i = 0; i < Player.buffType.Length; i++)
                if (Player.buffType[i] > 0 && Main.debuff[Player.buffType[i]] && Player.buffTime[i] > 0)
                    Player.buffTime[i]--;
        }
    }

}
