using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon
{
    public class TyphonArrow : ModProjectile
    {
        public bool Vanity;
        public bool Vanity2;
        public bool Stuck;
        public int  StuckNpcIndex = -1;
        public Vector2 StuckOffset;

        private const float RainHomingTurnDeg = 3.0f;   // 下落箭每次 update 最大偏转角（强追踪）
        private const float RainHomingRange   = 500f;   // 下落箭追踪半径（fallback）
        private const int   StuckLifeTicks    = 600;    // 10s × 60
        private const float NormalGravity     = 0.15f;  // 普攻箭每帧 Y 加速度（比 vanilla 弓箭低一半）
        private const float NormalArrowVx     = 12f;    // 普攻抛物线水平基础速度

        public override void SetStaticDefaults()
        {
            // 给下落箭准备长拖尾用的位置/旋转缓存
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
        }

        public override bool PreAI()
        {
            //插在敌人身上
            if (Stuck)
            {
                if (StuckNpcIndex < 0 || StuckNpcIndex >= Main.maxNPCs)
                {
                    Projectile.Kill();
                    return false;
                }
                NPC host = Main.npc[StuckNpcIndex];
                if (!host.active || host.life <= 0)
                {
                    Projectile.Kill();
                    return false;
                }
                Projectile.Center  = host.Center + StuckOffset;
                Projectile.velocity = Vector2.Zero;
                return false;
            }

            if (Vanity)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                if (Projectile.alpha < 127)
                    Projectile.alpha += 7;

                if (Projectile.alpha > 127)
                    Projectile.alpha = 127;

                if (Projectile.timeLeft <= 2)
                {
                    Vanity = false;
                    Vanity2 = true;
                    Projectile.timeLeft = 60;
                }
                return false;
            }

            if (Vanity2)
            {
                if (Projectile.timeLeft % 12 == 0)
                {
                    float spreadX = Main.rand.NextFloat(-TyphonAimReticle.ReticleAreaRadius, TyphonAimReticle.ReticleAreaRadius);
                    Projectile.NewProjectile(
                        Projectile.GetSource_Death(),
                        new Vector2(Projectile.ai[0] + spreadX, Projectile.ai[1] - 1000),
                        Projectile.velocity.Length() * Vector2.UnitY * Main.rand.NextFloat(0.95f, 1.05f) / 2,
                        Type, Projectile.damage, Projectile.knockBack, Projectile.owner,
                        Projectile.ai[0], Projectile.ai[1], 3f);
                }
                return false;
            }
            if (Projectile.ai[2] == 2f)
            {
                Projectile.position += Projectile.velocity;
                Projectile.rotation  = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                if (Main.rand.NextBool(2))
                {
                    Vector2 dustPos = Projectile.Center - Projectile.velocity * 0.4f;
                    Dust d = Dust.NewDustPerfect(
                        dustPos,
                        DustID.PurpleTorch,
                        Projectile.velocity * -0.05f + Main.rand.NextVector2Circular(0.6f, 0.6f),
                        100,
                        default,
                        Main.rand.NextFloat(1.0f, 1.4f));
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }
                return false;
            }
            if (Projectile.ai[2] == 3f)
            {
                if (Projectile.localAI[0] == 0f)
                {
                    Vector2 reticleCenter = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                    NPC pick = PickRandomEnemyInArea(reticleCenter, TyphonAimReticle.ReticleAreaRadius);
                    Projectile.localAI[0] = pick != null ? pick.whoAmI + 1f : -1f; // -1 = 区域内无目标
                }

                NPC tgt = null;
                if (Projectile.localAI[0] > 0f)
                {
                    int idx = (int)Projectile.localAI[0] - 1;
                    if (idx >= 0 && idx < Main.maxNPCs)
                    {
                        NPC pinned = Main.npc[idx];
                        if (pinned.active && !pinned.friendly && pinned.life > 0 && !pinned.dontTakeDamage)
                            tgt = pinned;
                    }
                }
                // 区域内目标失效或本来就空：回退到附近最近敌人
                if (tgt == null) tgt = FindNearestEnemy(RainHomingRange);

                if (tgt != null)
                {
                    float currentAngle = Projectile.velocity.ToRotation();
                    float targetAngle  = (tgt.Center - Projectile.Center).ToRotation();
                    float diff         = MathHelper.WrapAngle(targetAngle - currentAngle);
                    float maxTurn      = MathHelper.ToRadians(RainHomingTurnDeg);
                    Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Clamp(diff, -maxTurn, maxTurn));
                }
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center - Projectile.velocity * 0.3f,
                        DustID.PurpleTorch,
                        Projectile.velocity * -0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        140,
                        default,
                        Main.rand.NextFloat(0.9f, 1.3f));
                    d.noGravity = true;
                    d.fadeIn = 1.0f;
                }
                if (Main.rand.NextBool(8))
                {
                    Dust spark = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.UltraBrightTorch,
                        Main.rand.NextVector2Circular(1.2f, 1.2f),
                        180,
                        Color.MediumPurple,
                        Main.rand.NextFloat(0.6f, 1.0f));
                    spark.noGravity = true;
                }

                return base.PreAI();
            }

            // 普攻
            if (Projectile.ai[2] == 0f)
            {
                Projectile.velocity.Y += NormalGravity;
                Projectile.rotation    = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                // 白色粒子拖尾
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center - Projectile.velocity * 0.3f,
                        DustID.WhiteTorch,
                        Projectile.velocity * -0.05f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                        100,
                        new Color(150, 150, 150),
                        Main.rand.NextFloat(0.8f, 1.2f));
                    d.noGravity = true;
                    d.fadeIn = 1.0f;
                }
                return false;
            }

            // S3 本体追加箭
            if (Projectile.ai[2] == 4f)
            {
                Projectile.position += Projectile.velocity;
                Projectile.rotation  = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center - Projectile.velocity * 0.3f,
                        DustID.UltraBrightTorch,
                        Main.rand.NextVector2Circular(0.6f, 0.6f),
                        120,
                        Color.MediumPurple,
                        Main.rand.NextFloat(1.0f, 1.4f));
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }
                return false;
            }

            return base.PreAI();
        }

        public override bool ShouldUpdatePosition()
        {
            return !Vanity2 && !Stuck;
        }

        public override bool? CanDamage()
        {
            if (Vanity || Stuck)
                return false;
            return base.CanDamage();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (Vanity || Projectile.ai[2] == 3f)
            {
                Color color = Color.MediumPurple;
                color.A = 250;
                return color;
            }
            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[2] != 3f || Vanity || Vanity2 || Stuck)
                return true;

            Texture2D px = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            int len = ProjectileID.Sets.TrailCacheLength[Type];
            Color baseTint = new Color(180, 100, 255);
            var src = new Rectangle(0, 0, 1, 1);
            var leftOrigin = new Vector2(0f, 0.5f);
            Vector2 prev = Projectile.Center;
            for (int i = 0; i < len; i++)
            {
                Vector2 cur = Projectile.oldPos[i] + Projectile.Size / 2f;
                if (Projectile.oldPos[i] == Vector2.Zero) break;

                float t = 1f - (float)i / len;
                Vector2 seg = cur - prev;
                float dist = seg.Length();
                if (dist < 0.5f) { prev = cur; continue; }
                float rot = seg.ToRotation();
                Vector2 drawPos = prev - Main.screenPosition;
                Main.spriteBatch.Draw(px, drawPos, src,
                    baseTint * (t * 0.32f), rot, leftOrigin,
                    new Vector2(dist, 20f * t + 5f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(px, drawPos, src,
                    baseTint * (t * 0.60f), rot, leftOrigin,
                    new Vector2(dist, 12f * t + 3f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(px, drawPos, src,
                    baseTint * (t * 1.0f), rot, leftOrigin,
                    new Vector2(dist, 5f * t + 1.5f), SpriteEffects.None, 0f);

                prev = cur;
            }
            return true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[2] == 3f)
            {
                int stack = CountStuckArrowsOn(target);
                if (stack > 0)
                    modifiers.SourceDamage *= 1f + 0.07f * Math.Min(stack,30);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Vanity || Stuck) return;
            if (Projectile.ai[2] != 3f) return;

            Stuck         = true;
            StuckNpcIndex = target.whoAmI;
            StuckOffset   = Projectile.Center - target.Center;
            Projectile.velocity     = Vector2.Zero;
            Projectile.tileCollide  = false;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft     = StuckLifeTicks;
            Projectile.netUpdate    = true;

            // 计入本箭后，若敌人身上累积 ≥10 → 本体追射一支 100% 攻击力箭
            if (Main.myPlayer == Projectile.owner && CountStuckArrowsOn(target) >= 10)
            {
                Player owner = Main.player[Projectile.owner];
                Vector2 dir = (target.Center - owner.Center).SafeNormalize(Vector2.UnitX);
                const float bonusSpeed = 22f;
                Projectile.NewProjectile(
                    owner.GetSource_FromThis(),
                    owner.Center,
                    dir * bonusSpeed,
                    Type,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    0f, 0f, 4f);
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            // 不再依赖 source 类型，纯按 ai[2] 模式分派（让 TyphonStar 也能触发 Vanity）
            float mode = Projectile.ai[2];
            if (mode == 1f)
            {
                // S3 标记箭 → Vanity 流程
                Vanity = true;
                Projectile.timeLeft = 120;
                Projectile.alpha = 255;
                return;
            }
            if (mode == 2f)
            {
                // S1：禁用 vanilla aiStyle（无重力），手动维持直线飞行
                Projectile.aiStyle = -1;
                Projectile.tileCollide = true;
                return;
            }
            if (mode == 3f)
            {
                // S3 下落箭：penetrate=-1 让其命中不死，由 OnHitNPC 切到 Stuck
                Projectile.penetrate = -1;
                return;
            }
            if (mode == 4f)
            {
                // S3 本体追加箭：禁用 vanilla aiStyle，直射敌人
                Projectile.aiStyle = -1;
                Projectile.tileCollide = false;
                Projectile.penetrate = 1;
                Projectile.timeLeft = 90;
                Projectile.extraUpdates = 1;
                return;
            }
            // mode == 0：普攻抛物线弹道
            Projectile.tileCollide  = true;
            Projectile.aiStyle      = -1;
            Projectile.extraUpdates = 0;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Vanity || Main.myPlayer != Projectile.owner)
                return;
            // S3 标记箭被提前打死时，仍至少落一发雨点
            float spreadX = Main.rand.NextFloat(-TyphonAimReticle.ReticleAreaRadius, TyphonAimReticle.ReticleAreaRadius);
            Projectile.NewProjectile(
                Projectile.GetSource_Death(),
                new Vector2(Projectile.ai[0] + spreadX, Projectile.ai[1] - 1000),
                Projectile.velocity.Length() * Vector2.UnitY * Main.rand.NextFloat(0.95f, 1.05f) / 2,
                Type, Projectile.damage, Projectile.knockBack, Projectile.owner,
                Projectile.ai[0], Projectile.ai[1], 3f);
        }

        // ─── 帮助函数 ──────────────────────────────────────────────────
        private NPC FindNearestEnemy(float range)
        {
            NPC best = null;
            float bestDistSq = range * range;
            foreach (Projectile p in Main.projectile)
            {
                if (!p.active) continue;
                if (p.owner != Projectile.owner) continue;
                if (p.type != ModContent.ProjectileType<TyphonAimReticle>()) continue;
                if (p.Center == null) return null;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy(Projectile)) continue;
                    
                    float d = Vector2.DistanceSquared(npc.Center, p.Center);
                    if (d < bestDistSq)
                    {
                        bestDistSq = d;
                        best = npc;
                    }
                }
                break;
            }
            
            return best;
        }

        private static NPC PickRandomEnemyInArea(Vector2 center, float radius)
        {
            float r2 = radius * radius;
            int count = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.life <= 0 || npc.dontTakeDamage) continue;
                if (Vector2.DistanceSquared(npc.Center, center) > r2) continue;
                count++;
            }
            if (count == 0) return null;
            int pick = Main.rand.Next(count);
            int seen = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.life <= 0 || npc.dontTakeDamage) continue;
                if (Vector2.DistanceSquared(npc.Center, center) > r2) continue;
                if (seen == pick) return npc;
                seen++;
            }
            return null;
        }

        private int CountStuckArrowsOn(NPC target)
        {
            int count = 0;
            int targetWho = target.whoAmI;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != Type) continue;
                if (p.ModProjectile is TyphonArrow ta && ta.Stuck && ta.StuckNpcIndex == targetWho)
                    count++;
            }
            return count;
        }
    }
}
