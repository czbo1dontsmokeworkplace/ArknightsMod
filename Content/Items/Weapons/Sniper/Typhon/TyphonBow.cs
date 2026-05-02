using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Projectiles.Sniper.Typhon;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Typhon
{
    public class TyphonBow : UpgradeWeaponBase
    {
        private const int BaseUseTime = 60;

        private static SoundStyle SkillActiveSound;

        public override void Load()
        {
            SkillActiveSound = new SoundStyle("ArknightsMod/Sounds/SkillActive1")
            {
                Volume = 0.4f,
                MaxInstances = 4,
            };
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient<PolymerizationPreparation>(4);
            recipe.AddIngredient<RefinedSolvent>(7);
            recipe.AddTile(ModContent.TileType<FactoryTile>());
            recipe.Register();
        }

        public override void SetDefaults()
        {
            Item.damage = 209;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = 8;
            Item.useAmmo = AmmoID.Arrow;
            Item.noMelee = true;
        }

        public override Vector2? HoldoutOffset() => new(-8, 0);

        public override bool AltFunctionUse(Player player) => true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // 每次攻击动画期间，让武器以基础瞄准角为基准额外旋转一圈（2π）
            // itemAnimation 从 itemAnimationMax 倒数到 0 → t 从 0 增到 1
            if (player.itemAnimationMax <= 0 || player.itemAnimation <= 0) return;
            float t = (float)player.itemAnimation / player.itemAnimationMax;
            if (t > 0.5f)
            {
                // 前半段（射出后 0~0.5s）：转一圈
                // spinProgress：0 = 刚射出，1 = 转完一圈
                float spinProgress = (1f - t) / 0.5f;  // 等价于 2*(1-t)
                float spin = spinProgress * MathHelper.TwoPi * player.direction * -1;
                player.itemRotation = spin;
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return base.CanUseItem(player);

            var modPlayer = player.GetModPlayer<WeaponPlayer>();

            // 默认每帧重置（避免 S1 加速 / 技能激活音效残留到普攻）
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.UseSound = SoundID.Item5; // vanilla 弓箭射击音；技能激活时再覆盖

            if (player.altFunctionUse == 2)
            {
                // 右键 → 三技能均为手动激活
                if (modPlayer.Skill == 0 && modPlayer.StockCount > 0 && !modPlayer.SkillActive)
                {
                    // S1 激活
                    modPlayer.SkillActive = true;
                    modPlayer.SkillTimer = 0;
                    modPlayer.DelStockCount();
                    player.GetModPlayer<TyphonState>().S1Stacks = 0;

                    SoundEngine.PlaySound(SkillActiveSound, player.Center);
                    return false;
                }
                if (modPlayer.Skill == 1 && modPlayer.StockCount > 0 && !modPlayer.SkillActive)
                {
                    // S2 激活
                    modPlayer.SkillActive = true;
                    modPlayer.SkillTimer = 0;
                    modPlayer.DelStockCount();

                    // 第二次及以后激活：去掉自动结束，使持续时间无限
                    var ts = player.GetModPlayer<TyphonState>();
                    ts.S2ActivationCount++;
                    if (modPlayer.CurrentSkill != null)
                        modPlayer.CurrentSkill.AutoUpdateActive = ts.S2ActivationCount < 2;
                    if (ts.S2ActivationCount == 1)
                        ts.S2FirstHits = 0;

                    SoundEngine.PlaySound(SkillActiveSound, player.Center);
                    return false;
                }
                if (modPlayer.Skill == 2 && modPlayer.StockCount > 0 && !modPlayer.SkillActive)
                {
                    // S3 激活
                    modPlayer.SkillActive = true;
                    modPlayer.SkillTimer = 0;
                    modPlayer.DelStockCount();

                    SoundEngine.PlaySound(SkillActiveSound, player.Center);
                    return false;
                }
                return false;
            }

            // 左键攻击：S1 激活期间应用 +45% 攻速 + 每层 +5%
            if (modPlayer.Skill == 0 && modPlayer.SkillActive)
            {
                float speedMult = 1.45f + 0.05f * player.GetModPlayer<TyphonState>().S1Stacks;
                Item.useTime = (int)Math.Max(1, BaseUseTime / speedMult);
                Item.useAnimation = Item.useTime;
            }

            return base.CanUseItem(player);
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (Main.myPlayer != player.whoAmI) return;
            var modPlayer = player.GetModPlayer<WeaponPlayer>();
            // S1：基础 +45%，每个击杀叠 +5%
            if (modPlayer.Skill == 0 && modPlayer.SkillActive)
                damage *= 1.45f + 0.05f * player.GetModPlayer<TyphonState>().S1Stacks;
            if (modPlayer.Skill == 1 && modPlayer.SkillActive) damage *= 1.911f;
            if (modPlayer.Skill == 2 && modPlayer.SkillActive) damage *= 1.75f;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            var modPlayer = player.GetModPlayer<WeaponPlayer>();
            if (modPlayer.Skill == 2 && modPlayer.SkillActive)
            {
                // S3：把方向纠正为指向鼠标上方（保留原始的拋物逻辑）
                velocity = velocity.RotatedBy(-velocity.ToRotation())
                                   .RotatedBy((Main.MouseWorld + new Vector2(0, -1000) - position).ToRotation());
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var modPlayer = player.GetModPlayer<WeaponPlayer>();

            // 重算无浮动伤害：复刻 vanilla 全部 StatModifier 流程，但跳过 Main.DamageVar 的 ±15% 随机
            int cleanDamage = ComputeFixedDamage(player);

            // S3：发射 TyphonArrow 标记箭，到时点引发箭雨
            if (modPlayer.Skill == 2 && modPlayer.SkillActive)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Projectile.NewProjectile(
                        source, position, velocity * 2,
                        ModContent.ProjectileType<TyphonArrow>(),
                        cleanDamage, knockback, player.whoAmI,
                        Main.MouseWorld.X, Main.MouseWorld.Y, 1f /* ai[2]=1 → S3 标记 */);
                }
                return false;
            }

            if (modPlayer.Skill == 1 && modPlayer.SkillActive)
            {
                int s2Type = ModContent.ProjectileType<TyphonS2Arrow>();
                // 仍找两个不同目标，但只把目标 ID 交给箭，不再修改初速度方向
                FindTwoTargets(player, position, out NPC t1, out NPC t2);

                // 初速度严格按鼠标方向，仅做 ±5° 散射避免两支箭重叠
                Vector2 v1 = velocity.RotatedBy(MathHelper.ToRadians(-5));
                Vector2 v2 = velocity.RotatedBy(MathHelper.ToRadians(5));

                float ai0_1 = t1 != null ? t1.whoAmI + 1f : 0f;
                float ai0_2 = (t2 ?? t1) != null ? (t2 ?? t1).whoAmI + 1f : 0f;
                Projectile.NewProjectile(source, position, v1, s2Type, cleanDamage, knockback, player.whoAmI, ai0_1);
                Projectile.NewProjectile(source, position, v2, s2Type, cleanDamage, knockback, player.whoAmI, ai0_2);
                return false;
            }

            // S1 激活：TyphonArrow，ai[2]=2 → 无重力 + 紫色拖尾
            if (modPlayer.Skill == 0 && modPlayer.SkillActive)
            {
                Projectile.NewProjectile(
                    source, position, velocity,
                    ModContent.ProjectileType<TyphonArrow>(),
                    cleanDamage, knockback, player.whoAmI,
                    0f, 0f, 2f);
                return false;
            }

            // 默认（无技能激活）：单发 TyphonArrow（普通带重力箭矢）
            Projectile.NewProjectile(
                source, position, velocity,
                ModContent.ProjectileType<TyphonArrow>(),
                cleanDamage, knockback, player.whoAmI,
                0f, 0f, 0f);
            return false;
        }

        private int ComputeFixedDamage(Player player)
        {
            // 复刻 vanilla 伤害流程但去掉变量浮动：
            //   先取玩家对该伤害类的 StatModifier，再让 ModifyWeaponDamage 加 buff，
            //   最后乘到 Item.damage 上四舍五入；不调用 Main.DamageVar，所以无 ±15% 抖动。
            StatModifier mod = player.GetDamage(Item.DamageType);
            ModifyWeaponDamage(player, ref mod);
            return (int)Math.Round(mod.ApplyTo(Item.damage));
        }

        private static Vector2 AimVelocity(Vector2 from, Vector2 fallback, NPC target)
        {
            if (target == null) return fallback;
            Vector2 dir = target.Center - from;
            if (dir.LengthSquared() < 1f) return fallback;
            return Vector2.Normalize(dir) * fallback.Length();
        }

        private static void FindTwoTargets(Player player, Vector2 origin, out NPC t1, out NPC t2)
        {
            t1 = null;
            t2 = null;
            const float maxRange = 1200f;
            Vector2 aim = Main.MouseWorld;
            float best1 = float.MaxValue;
            float best2 = float.MaxValue;
            float maxRangeSq = maxRange * maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(player)) continue;
                if (Vector2.DistanceSquared(npc.Center, origin) > maxRangeSq) continue;
                float d = Vector2.DistanceSquared(npc.Center, aim);
                if (d < best1)
                {
                    best2 = best1; t2 = t1;
                    best1 = d;     t1 = npc;
                }
                else if (d < best2)
                {
                    best2 = d; t2 = npc;
                }
            }
        }

        /// <summary>
        /// 提丰武器自有的 ModPlayer 状态：
        ///   - S1 击杀叠加层数
        ///   - S2 激活次数（第二次起持续时间无限）
        ///   - S2 第一次激活期间的命中计数（结束后返还为 SP，上限 30）
        /// 不污染 WeaponPlayer。
        /// </summary>
        public class TyphonState : ModPlayer
        {
            public int S1Stacks;
            public int S2ActivationCount;
            public int S2FirstHits;
            private bool _wasFirstS2Active;

            public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
            {
                if (Player.HeldItem.ModItem is not TyphonBow) return;
                var wp = Player.GetModPlayer<WeaponPlayer>();
                if (!wp.SkillActive) return;

                if (wp.Skill == 0 && target.life <= 0)
                {
                    if (S1Stacks < 30) S1Stacks++;
                    wp.SkillTimer = Math.Max(0, wp.SkillTimer - 60 * 3);
                }
                else if (wp.Skill == 1 && S2ActivationCount == 1 && S2FirstHits < 30)
                {
                    S2FirstHits++;
                }
            }

            public override void PostUpdate()
            {
                var wp = Player.GetModPlayer<WeaponPlayer>();
                bool nowActive = Player.HeldItem.ModItem is TyphonBow
                    && wp.Skill == 1 && wp.SkillActive && S2ActivationCount == 1;

                // 第一次 S2 active 下降沿 → 把命中数兑换为 SP
                if (_wasFirstS2Active && !nowActive && S2FirstHits > 0 && wp.CurrentSkill != null)
                {
                    int gain = Math.Min(S2FirstHits, 30);
                    wp.SkillCharge = Math.Min(wp.SkillCharge + gain * wp.Div, wp.SkillChargeMax);
                    wp.SP          = Math.Min(wp.SP + gain, wp.CurrentSkill.CurrentLevelData.MaxSP);
                    S2FirstHits = 0;
                }
                _wasFirstS2Active = nowActive;

                // 切换到非提丰武器时复位 ActivationCount（替代旧的 WeaponPlayer 切武器复位）
                if (Player.HeldItem.ModItem is not TyphonBow)
                {
                    S2ActivationCount = 0;
                    S1Stacks = 0;
                    S2FirstHits = 0;
                }
            }
        }
    }
}
