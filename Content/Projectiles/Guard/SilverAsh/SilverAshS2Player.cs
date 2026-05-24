using ArknightsMod.Content.Items.Weapons.Guard.SilverAsh;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.SilverAsh
{
    public class SilverAshS2Player : ModPlayer
    {
        public bool yinhui2 = false;
        public override void ResetEffects()
        {
            if (yinhui2)
            {
                Player.statDefense *= 2f;
                Player.lifeRegen += (int)(Player.statLifeMax2 * 0.12f);
            }
            if (Main.myPlayer != Player.whoAmI)
                return;
            bool isHoldingTargetWeapon = Player.HeldItem.type == ModContent.ItemType<SilverAshWeapon>();
            if (!isHoldingTargetWeapon)
            {
                Player.GetModPlayer<SilverAshS2Player>().yinhui2 = false;
            }

        }
    }
    public class SilverAshS3Player : ModPlayer
    {
        public bool yinhui3 = false;
        public override void ResetEffects()
        {
            if (yinhui3 == true)
            {
                Player.statDefense *= 0.3f;
            }
            if (Main.myPlayer != Player.whoAmI)
                return;
            bool isHoldingTargetWeapon2 = Player.HeldItem.type == ModContent.ItemType<SilverAshWeapon>();
            if (!isHoldingTargetWeapon2)
            {
                Player.GetModPlayer<SilverAshS3Player>().yinhui3 = false;
            }

        }
    }
}
