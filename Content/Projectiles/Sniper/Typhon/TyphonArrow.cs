using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

// Projectile.ai[2] 模式枚举：
//   0 = 普通箭矢（默认带重力）
//   1 = S3 标记箭（Vanity → 上升 → Vanity2 落雨）
//   2 = S1 箭矢（无重力，紫色拖尾）
//   3 = S3 雨点箭（从天而降，弱追踪，命中后插在敌人身上 3.5s）
//   4 = S3 本体追加箭（10+ 触发，从玩家直射目标，单次命中无插入）

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon
{
    public class TyphonArrow : ModProjectile
    {
        public bool Vanity;
        public bool Vanity2;

        // 雨点箭命中后插在敌人身上的状态
        public bool Stuck;
        public int  StuckNpcIndex = -1;
        public Vector2 StuckOffset;

        private const float RainHomingTurnDeg = 0.6f;   // 雨点箭每次 update 最大偏转角（弱追踪）
        private const float RainHomingRange   = 600f;   // 雨点箭追踪半径
        private const int   StuckLifeTicks    = 210;    // 3.5s × 60

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
        }

        public override bool PreAI()
        {
            // ── 插在敌人身上 ─────────────────────────────────────────
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
                    // 标记 ai[2]=3 → 雨点箭
                    Projectile.NewProjectile(
                        Projectile.GetSource_Death(),
                        new Vector2(Projectile.ai[0], Projectile.ai[1]) - new Vector2(Main.rand.NextFloat(-16, 16), 1000),
                        Projectile.velocity.Length() * Vector2.UnitY * Main.rand.NextFloat(0.95f, 1.05f) / 2,
                        Type, Projectile.damage, Projectile.knockBack, Projectile.owner,
                        0f, 0f, 3f);
                }
                return false;
            }

            // S1 模式：无重力直线飞行 + 紫色拖尾
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

            // S3 雨点箭：弱追踪 + 让 vanilla 继续应用重力 / 旋转
            if (Projectile.ai[2] == 3f)
            {
                NPC tgt = FindNearestEnemy(RainHomingRange);
                if (tgt != null)
                {
                    float currentAngle = Projectile.velocity.ToRotation();
                    float targetAngle  = (tgt.Center - Projectile.Center).ToRotation();
                    float diff         = MathHelper.WrapAngle(targetAngle - currentAngle);
                    float maxTurn      = MathHelper.ToRadians(RainHomingTurnDeg);
                    Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Clamp(diff, -maxTurn, maxTurn));
                }

                // 紫色下落特效
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
                    // 偶发的紫蓝色"火星"
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

            // S3 本体追加箭：直射、不受重力影响、单次命中
            if (Projectile.ai[2] == 4f)
            {
                Projectile.position += Projectile.velocity;
                Projectile.rotation  = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                // 亮紫长尾粒子，与雨点箭区分
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
            // Vanity2（落雨标记）和 Stuck（插在敌人）都不应自由位移
            return !Vanity2 && !Stuck;
        }

        public override bool? CanDamage()
        {
            if (Vanity || Stuck)
                return false;
            return base.CanDamage();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 全模式禁暴击；雨点箭额外按目标身上已插箭数 +7%/箭
            modifiers.DisableCrit();
            if (Projectile.ai[2] == 3f)
            {
                int stack = CountStuckArrowsOn(target);
                if (stack > 0)
                    modifiers.SourceDamage *= 1f + 0.07f * stack;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 仅雨点箭命中后插在敌人身上 3.5s
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
                    Projectile.damage,        // 与雨点箭同伤害（= 100% × 玩家加成 base，无随机浮动）
                    Projectile.knockBack,
                    Projectile.owner,
                    0f, 0f, 4f);              // ai[2]=4 → 本体追加箭
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            float mode = Projectile.ai[2];
            if (source is EntitySource_ItemUse_WithAmmo)
            {
                if (mode == 1f)
                {
                    // S3 标记箭 → Vanity 流程
                    Vanity = true;
                    Projectile.timeLeft = 120;
                    Projectile.alpha = 255;
                }
                else if (mode == 2f)
                {
                    // S1：禁用 vanilla aiStyle（无重力），手动维持直线飞行
                    Projectile.aiStyle = -1;
                    Projectile.tileCollide = true;
                }
                else
                {
                    // 普攻：恢复瓦片碰撞
                    Projectile.tileCollide = true;
                }
                return;
            }

            // 由 Vanity2 召唤的雨点箭
            if (mode == 3f)
            {
                // 让箭命中后能不死亡，由 OnHitNPC 切到 Stuck 状态
                Projectile.penetrate = -1;
            }
            // S3 本体追加箭：禁用 vanilla aiStyle，直射敌人
            else if (mode == 4f)
            {
                Projectile.aiStyle = -1;
                Projectile.tileCollide = false;   // 不被墙挡住
                Projectile.penetrate = 1;         // 单次命中
                Projectile.timeLeft = 90;
                Projectile.extraUpdates = 1;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Vanity || Main.myPlayer != Projectile.owner)
                return;
            // S3 标记箭被提前打死时，仍至少落一发雨点
            Projectile.NewProjectile(
                Projectile.GetSource_Death(),
                new Vector2(Projectile.ai[0], Projectile.ai[1]) - new Vector2(Main.rand.NextFloat(-16, 16), 1000),
                Projectile.velocity.Length() * Vector2.UnitY * Main.rand.NextFloat(0.95f, 1.05f) / 2,
                Type, Projectile.damage, Projectile.knockBack, Projectile.owner,
                0f, 0f, 3f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.MediumPurple;
            color.A = 250;
            if (!Vanity)
                color.A = lightColor.A;
            return color;
        }

        // ─── 帮助函数 ──────────────────────────────────────────────────
        private NPC FindNearestEnemy(float range)
        {
            NPC best = null;
            float bestDistSq = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile)) continue;
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = npc;
                }
            }
            return best;
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
