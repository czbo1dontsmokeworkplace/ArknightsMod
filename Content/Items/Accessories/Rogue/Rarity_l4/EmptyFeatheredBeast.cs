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
            Item.value = Item.sellPrice(16, 0, 0, 0); // 15½ð±Ò
            Item.rare = ItemRarityID.Master;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Ô­ÓÐÐ§¹û
            player.GetDamage(DamageClass.Summon) += 0.30f; // ÕÙ»½ÉËº¦+30%
            player.GetCritChance(DamageClass.Summon) += 5; 
            player.GetModPlayer<SummonerEmblemPlayer>().whipDamageBoost = 0.60f;
        }
    }

    public class SummonerEmblemPlayer : ModPlayer
    {
        public float whipDamageBoost;

        public override void ResetEffects()
        {
            whipDamageBoost = 0f;
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            // ¼ì²â±Þ×ÓÎäÆ÷
            if (item.CountsAsClass(DamageClass.SummonMeleeSpeed))
            {
                modifiers.FinalDamage *= 1f + whipDamageBoost;
            }
        }
    }
}