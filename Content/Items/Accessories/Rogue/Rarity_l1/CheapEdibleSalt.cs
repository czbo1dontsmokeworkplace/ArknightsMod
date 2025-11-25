using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l1
{
    public class CheapEdibleSalt : ModItem
    {
        //加7%伤害（最终乘算）
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.value = Item.sellPrice(4, 0, 0, 0); // 8金币价值
            Item.rare = ItemRarityID.Green; // 浅紫色稀有度
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<Vulnerability1Player>().damageAmplifierActive = true;
        }
    }

    public class Vulnerability1Player : ModPlayer
    {
        public bool damageAmplifierActive;

        public override void ResetEffects()
        {
            damageAmplifierActive = false;
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!damageAmplifierActive) return;

            if (item.DamageType == DamageClass.Melee || item.DamageType == DamageClass.Ranged)
            {
                modifiers.FinalDamage *= 1.07f;
            }
        }
		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (!damageAmplifierActive)
				return;

			// 检测法术或召唤伤害
			if (proj.DamageType == DamageClass.Melee || proj.DamageType == DamageClass.Ranged) {
				modifiers.FinalDamage *= 1.07f;
			}
		}

	}
}
