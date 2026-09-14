using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4
{
    public class BrightWeeping : ModItem
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
            player.GetModPlayer<ArcaneAmplifierPlayer>().damageAmplifierActive = true;
        }
    }

    public class ArcaneAmplifierPlayer : ModPlayer
    {
        public bool damageAmplifierActive;

        public override void ResetEffects()
        {
            damageAmplifierActive = false;
        }

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (!damageAmplifierActive)
				return;

			// 检测法术武器
			if (item.DamageType == DamageClass.Magic) {
				modifiers.FinalDamage *= 1.40f;
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (!damageAmplifierActive)
				return;

			// 检测法术或召唤伤害
			if (proj.DamageType == DamageClass.Magic || proj.DamageType == DamageClass.Summon) {
				modifiers.FinalDamage *= 1.40f;
			}
		}


	}
}