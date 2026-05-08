using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon
{
    /// <summary>
    /// 提丰 S3 前摇视觉：弓前方一颗闪烁的紫色四芒星，存在 0.5s 后消失，
    /// 消失瞬间在 OnKill 中生成真正的 S3 标记箭（TyphonArrow ai[2]=1）。
    /// ai[0], ai[1] = 鼠标世界坐标（标记目标）
    /// </summary>
    public class TyphonStar : ModProjectile
    {
        // 总存在时长 = 抬弓 0.15 + 蓄力 0.25 + 放弓 0.11 = 0.51 × useAnimation
        // 前 DelayTicks 帧（抬弓阶段）不渲染、不计 fade；之后才显示并计算 lifeT
        private int DelayTicks;
        private int TotalTicks;
        private int StarLifeTicks;   // 可见阶段 = TotalTicks - DelayTicks
        private const float ForwardOffset = 60f;

        public override void SetDefaults()
        {
            Projectile.width        = 40;
            Projectile.height       = 40;
            Projectile.aiStyle      = -1;
            Projectile.tileCollide  = false;
            Projectile.friendly     = false;
            Projectile.hostile      = false;
            Projectile.penetrate    = -1;
            Projectile.extraUpdates = 0;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            // 弓前方斜上 30 像素，沿玩家朝向
            Vector2 forwardUp = new Vector2(owner.direction, -1f);
            forwardUp = forwardUp.SafeNormalize(Vector2.UnitX);
            Projectile.Center   = owner.MountedCenter + forwardUp * ForwardOffset;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 抬弓阶段：星还没出现，跳过绘制
            int elapsed = TotalTicks - Projectile.timeLeft;
            if (elapsed < DelayTicks) return false;

            Texture2D px = TextureAssets.MagicPixel.Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            // lifeT 只对可见阶段计算（0 = 抬弓刚结束星出现，1 = 星即将消失发射箭）
            int visibleElapsed  = elapsed - DelayTicks;
            float lifeT = StarLifeTicks > 0 ? (float)visibleElapsed / StarLifeTicks : 1f;
            // fade: 前 70% 长大，后 30% 收回
            float fade = lifeT < 0.7f ? lifeT / 0.7f : (1f - lifeT) / 0.3f;
            fade = MathHelper.Clamp(fade, 0f, 1f);
            // 高频脉冲
            float pulse = 0.5f + 0.5f * MathF.Sin(lifeT * MathHelper.TwoPi * 8f);

            float armLen   = 60f * fade;
            float armCore  =  1.5f * fade;       // 白色锐线宽
            float armSoft  =  4f   * fade;       // 白色柔边宽
            float bloomLen = 40f * fade;
            float bloomThk = 16f * fade * (0.85f + 0.15f * pulse);
            float coreSize =  8f * fade;

            // 紫色光晕（朝 8 个方向叠出近似圆形），低 alpha 营造 bloom
            Color bloom = new Color(170, 90, 255) * (0.35f * fade);
            Color soft  = new Color(220, 180, 255) * (0.65f * fade);
            Color core  = new Color(255, 240, 255) * fade;

            var src    = new Rectangle(0, 0, 1, 1);
            var origin = new Vector2(0.5f, 0.5f);

            // ── 1. 紫色径向 bloom（8 方向粗短紫条相互叠加 → 近圆光斑）─────────
            for (int i = 0; i < 4; i++)
            {
                float ang = i * MathHelper.PiOver4;
                Main.spriteBatch.Draw(px, center, src, bloom, ang, origin,
                    new Vector2(bloomLen, bloomThk), SpriteEffects.None, 0);
            }

            // ── 2. 白色十字四芒（长臂 0°/90° + 锐芯 + 柔边）────────────────
            for (int i = 0; i < 2; i++)
            {
                float ang = i * MathHelper.PiOver2;
                // 柔边
                Main.spriteBatch.Draw(px, center, src, soft, ang, origin,
                    new Vector2(armLen, armSoft), SpriteEffects.None, 0);
                // 锐芯
                Main.spriteBatch.Draw(px, center, src, core, ang, origin,
                    new Vector2(armLen, armCore), SpriteEffects.None, 0);
            }

            // ── 3. 中心高亮能量核（圆形小亮点）─────────────────────────────
            Main.spriteBatch.Draw(px, center, src, core * 1.3f, 0f, origin,
                new Vector2(coreSize), SpriteEffects.None, 0);

            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            int interval = owner.itemAnimationMax;
            if (interval <= 0)
                interval = owner.HeldItem.useAnimation;

            // 与 TyphonBow.UseStyle 对齐：抬弓 15%、蓄力 25%、放弓 11%，后摇 49%
            DelayTicks    = (int)(interval * 0.15f);
            TotalTicks    = Math.Max(8, (int)(interval * 0.51f));
            StarLifeTicks = Math.Max(1, TotalTicks - DelayTicks);

            Projectile.timeLeft = TotalTicks;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner) return;
            Player owner = Main.player[Projectile.owner];

            // 0.8 时点：发射 S3 标记箭 + 同时触发弓的射击音
            SoundEngine.PlaySound(SoundID.Item5, owner.Center);

            // 优先取当前瞄准框位置；找不到则回退到 ai[0]/ai[1]（鼠标位置）
            Vector2 target = TyphonAimReticle.GetCurrentPos(owner.whoAmI)
                             ?? new Vector2(Projectile.ai[0], Projectile.ai[1]);

            Vector2 dir = (target + new Vector2(0, -1000) - owner.Center).SafeNormalize(-Vector2.UnitY);
            Vector2 vel = dir * 16f;
            Projectile.NewProjectile(
                owner.GetSource_FromThis(),
                owner.Center,
                vel,
                ModContent.ProjectileType<TyphonArrow>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                target.X, target.Y, 1f);
        }
    }
}
