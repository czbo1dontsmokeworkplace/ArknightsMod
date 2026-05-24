using ArknightsMod.Content.Items.Weapons.Guard.Thorns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Thorns
{
    public class ThornsCounterPlayer : ModPlayer
    {
        public bool JiCi2 = false;
        public override void ResetEffects()
        {
            if (JiCi2 == true)
            {
                if (JiCi2_JSQ > 0) JiCi2_JSQ++;
                if (JiCi2_JSQ > 36) JiCi2_JSQ = 0;
                Player.statDefense *= 2.1f;
                Player.controlUseItem = false;
                Player.itemAnimation = 0;
                Player.itemTime = 0;
            }
            if (Main.myPlayer != Player.whoAmI)
                return;
            bool isHoldingTargetWeapon = Player.HeldItem.type == ModContent.ItemType<ThornsWeapon>();
            if (!isHoldingTargetWeapon)
            {
                Player.GetModPlayer<ThornsCounterPlayer>().JiCi2 = false;
            }

        }

        public int JiCi2_JSQ = 0;
        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (JiCi2 && JiCi2_JSQ == 0)
            {
                float js = Player.HeldItem.type == ModContent.ItemType<ThornsWeapon>() ? 1.6f : .8f;
                JiCi2_JSQ = 1;
                Vector2 velocity = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 10f;
                SoundStyle SoundStyle1 = new SoundStyle("ArknightsMod/Sounds/JiCi2");
                SoundEngine.PlaySound(SoundStyle1);
                Projectile.NewProjectile(Player.GetSource_Death(), Player.Center,
                velocity, ModContent.ProjectileType<ThornsCounter>(), (int)(Player.HeldItem.damage * js), Player.HeldItem.knockBack, Main.myPlayer);
            }
        }
        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (JiCi2 && JiCi2_JSQ == 0)
            {
                float js = Player.HeldItem.type == ModContent.ItemType<ThornsWeapon>() ? 1.6f : .8f;
                JiCi2_JSQ = 1;
                Vector2 velocity = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.Zero) * 10f;
                SoundStyle SoundStyle1 = new SoundStyle("ArknightsMod/Sounds/JiCi2");
                SoundEngine.PlaySound(SoundStyle1);
                Projectile.NewProjectile(Player.GetSource_Death(), Player.Center,
                velocity, ModContent.ProjectileType<ThornsCounter>(), (int)(Player.HeldItem.damage * js), Player.HeldItem.knockBack, Main.myPlayer);
            }
        }
    }
}
