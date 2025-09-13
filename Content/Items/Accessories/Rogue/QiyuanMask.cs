using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue
{
    public class QiyuanMask : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.value = Item.sellPrice(12, 0, 0, 0);
            Item.rare = ItemRarityID.Purple;
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<QiyuanMaskPlayer>().hasEmblem = true;
        }
    }

    public class QiyuanMaskPlayer : ModPlayer
    {
        public bool hasEmblem;

        public override void ResetEffects()
        {
            hasEmblem = false;
        }

        // 减少所有NPC对玩家的伤害
        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (hasEmblem && npc != null && !npc.friendly)
            {
                // 减少17%伤害（乘算）
                modifiers.SourceDamage *= 0.88f;

                // 添加视觉反馈（可选）
                
            }
        }

        // 减少所有NPC弹幕对玩家的伤害
        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (hasEmblem && proj != null && proj.hostile && proj.npcProj)
            {
                // 减少17%伤害（乘算）
                modifiers.SourceDamage *= 0.88f;

                // 添加视觉反馈（可选）
                
            }
        }
    }
}