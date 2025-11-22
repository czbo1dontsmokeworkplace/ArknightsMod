using System;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Defender.NoirCorne;

namespace ArknightsMod.Content.Projectiles.Defender.NoirCorne
{
    public class NoirShield_Projectile : ModProjectile
    {
        Player player => Main.player[Projectile.owner];
        Item item => player.HeldItem;

        public override void SetDefaults()
        {
            Projectile.hide = true;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void AI()
        {
            // 如果玩家不合法或没有拿 NoirShield 则删除投射物
            if (!player.active || player.dead || item.type != ModContent.ItemType<NoirShield>())
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.ai[1] <= 0)
            {
                float t = Math.Clamp(Projectile.ai[0] / 40f, 0, 1);

                Projectile.rotation = Projectile.rotation.AngleLerp(0.2f * player.direction, t);
                Projectile.Center = Vector2.Lerp(
                    Projectile.Center,
                    player.MountedCenter + new Vector2(-5 * player.direction, 6 + player.gfxOffY),
                    t
                );

                Projectile.ai[0]++;

                // 判断右键触发推盾
                if (Main.mouseRight)
                {
                    player.direction = Main.MouseWorld.X > player.Center.X ? 1 : -1;
                    Projectile.ai[0] = 0;
                    Projectile.ai[1] += 0.1f;
                }

                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, 0);
            }
            else
            {
                player.itemAnimation = player.itemTime = 4;

                // 如果还在按右键，推盾动画继续增强
                if (Main.mouseRight)
                    Projectile.ai[1] = MathHelper.Lerp(Projectile.ai[1], 1f, 0.07f);
                else
                    Projectile.ai[1] -= 0.1f;

                // 正前方的位移
                Projectile.rotation = Projectile.rotation.AngleLerp(0, Projectile.ai[1]);
                Projectile.Center = Vector2.Lerp(
                    Projectile.Center,
                    player.MountedCenter + new Vector2(12 * player.direction, player.gfxOffY),
                    Projectile.ai[1]
                );

                // 完全伸手
                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -MathHelper.PiOver2 * player.direction);
            }

            // 重置 timeLeft 让盾存在
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                tex.Size() / 2,
                0.9f,
                player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                0
            );

            return false;
        }
    }

	public class NoirShield_MainAtk : ModProjectile
{
    Player player => Main.player[Projectile.owner];

    public override void SetDefaults()
    {
        Projectile.hide = true;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ownerHitCheck = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.timeLeft = 2;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        // 结束条件
        if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<NoirShield>())
        {
            Projectile.Kill();
            return;
        }

        // 左键按住 = 主攻击循环
        bool attacking = Main.mouseLeft && player.channel;

        if (!attacking)
        {
            Projectile.ai[0] = 0;
            Projectile.ai[1] = 0;
            return;
        }

        float speed = 0.06f;

        // ai[1] = 0 回撤  / 1 推刺
        if (Projectile.ai[1] == 1)
        {
            Projectile.ai[0] += speed;
            if (Projectile.ai[0] >= 1f)
            {
                Projectile.ai[0] = 1f;
                Projectile.ai[1] = 0;
            }
        }
        else
        {
            Projectile.ai[0] -= speed;
            if (Projectile.ai[0] <= 0f)
            {
                Projectile.ai[0] = 0f;
                Projectile.ai[1] = 1;
            }
        }

        float t = Projectile.ai[0];
        int dir = player.direction;

        Vector2 backPos = player.MountedCenter + new Vector2(-8 * dir, 10);
        Vector2 frontPos = player.MountedCenter + new Vector2(20 * dir, -6);

        Projectile.Center = Vector2.Lerp(backPos, frontPos, t);

        Projectile.rotation = MathHelper.Lerp(0.3f * dir, 0, t);

        if (t < 0.5f)
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, 0);
        else
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -MathHelper.PiOver2 * dir);

        Projectile.timeLeft = 2;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var tex = TextureAssets.Projectile[Type].Value;

        Main.spriteBatch.Draw(tex,
            Projectile.Center - Main.screenPosition,
            null,
            lightColor,
            Projectile.rotation,
            tex.Size() * 0.5f,
            1f,
            player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
            0);

        return false;
    }
}

}
