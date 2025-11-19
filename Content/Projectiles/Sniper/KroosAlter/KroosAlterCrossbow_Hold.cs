using ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.KroosAlter
{
    public class KroosAlterCrossbow_Hold : ModProjectile
    {
        ref float Skill => ref Projectile.ai[2];

        ref float counter => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 28;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.coldDamage = true;
            Projectile.hide = true;
        }

        public override void AI()
        {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			Player player = Main.player[Projectile.owner];

            if (player.dead || !player.active || player.HeldItem.type != ModContent.ItemType<KroosAlterCrossbow>() || !player.channel)
            {
                Projectile.Kill();
                return;
            }



            if (player.channel)
            {
                Projectile.timeLeft = 2;

                Projectile.ai[0]++;

                #region 一技能
                if (modPlayer.Skill == 0 && modPlayer.SkillActive)
                {
                    if (Projectile.ai[0] < player.HeldItem.useTime)
                    {
                        int interval = player.HeldItem.useTime / 2;

                        if (Projectile.ai[0] % interval == 0)
                        {
                            //射弹
                            ShootBullet(player, MathHelper.ToRadians(2));
                            //爆炸粒子效果
                            ShootEffect(player);
                            //爆炸圆环效果
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                                Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 16,
                                Projectile.velocity.SafeNormalize(Vector2.Zero),
                                ModContent.ProjectileType<KroostheKeenGlint_Crossbow_Circle1>(),
                                0,
                                0,
                                player.whoAmI,
                                0,
                                0,
                                Skill
                                );
                            //音效
                            SoundStyle SkillSound = new SoundStyle("ArknightsMod/Sounds/p_atk_krossbow_h") with
                            {
                                Volume = 0.5f
                            };
                            SoundEngine.PlaySound(SkillSound, Projectile.position);

                            Projectile.ai[1]++;

                            if (Projectile.ai[1] >= 4)
                            {
                                Projectile.ai[0] = player.HeldItem.useTime; // 进入useDelay阶段
                                Projectile.ai[1] = 0;
                            }
                        }
                    }
                    // 冷却阶段处理
                    else if (Projectile.ai[0] >= player.HeldItem.useTime + player.HeldItem.reuseDelay)
                    {
                        Projectile.ai[0] = 0;
                    }
                }
                #endregion
                #region 二技能
                else if (modPlayer.Skill == 1 && modPlayer.SkillActive)
                {
					int shotsPerInterval = counter < 32 ? 2 : 4;
                    if (Projectile.ai[0] < player.HeldItem.useTime)
                    {
                        int interval = player.HeldItem.useTime / shotsPerInterval;

                        if (Projectile.ai[0] % interval == 0)
                        {
                            //射弹
                            ShootBullet(player, MathHelper.ToRadians(2));
                            //爆炸粒子效果
                            ShootEffect(player);
                            //爆炸圆环效果
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                                Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 16,
                                Projectile.velocity.SafeNormalize(Vector2.Zero),
                                ModContent.ProjectileType<KroostheKeenGlint_Crossbow_Circle1>(),
                                0,
                                0,
                                player.whoAmI,
                                0,
                                0,
                                Skill
                                );
                            //音效
                            SoundStyle SkillSound = new SoundStyle("ArknightsMod/Sounds/p_atk_krossbow_n") with
                            {
                                Volume = 0.5f
                            };
                            SoundEngine.PlaySound(SkillSound, Projectile.position);

                            Projectile.ai[1]++;
                            Projectile.localAI[0]++;
                            //Main.NewText(Projectile.localAI[0]);

                            if (Projectile.ai[1] >= 4)
                            {
                                Projectile.ai[0] = player.HeldItem.useTime; // 进入useDelay阶段
                                Projectile.ai[1] = 0;
                            }
                        }
                    }
                    // 冷却阶段处理
                    else if (Projectile.ai[0] >= player.HeldItem.useTime + player.HeldItem.reuseDelay)
                    {
                        Projectile.ai[0] = 0;
                    }
                }
                #endregion
                #region 普攻
                else
                {
                    if (Projectile.ai[0] >= player.HeldItem.useTime + player.HeldItem.reuseDelay)
                    {
                        ShootBullet(player, 0);
                        ShootEffect(player);

                        SoundStyle AttackSound = new SoundStyle("ArknightsMod/Sounds/p_atk_krossbow_d") with
                        {
                            Volume = 0.5f
                        };
                        SoundEngine.PlaySound(AttackSound, Projectile.position);

                        Projectile.ai[0] = 0;
                    }
                }
                #endregion
            }
            var vel = Vector2.Normalize(Main.MouseWorld - player.Center);
            Projectile.velocity = vel * 24;
            Projectile.position = player.RotatedRelativePoint(player.MountedCenter, true) - Projectile.Size / 2f;
            Projectile.direction = Projectile.spriteDirection = Projectile.velocity.X > 0 ? 1 : -1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;
            player.ChangeDir(Projectile.direction);
            player.itemRotation = player.itemRotation = (float)Math.Atan2(Projectile.velocity.Y * Projectile.direction, Projectile.velocity.X * Projectile.direction);//修改玩家的手持弹幕的方向
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
        }

        private void ShootBullet(Player player, float rot)
        {
			var modPlayer = Main.LocalPlayer.GetModPlayer<WeaponPlayer>();
			if (player.HasAmmo(player.inventory[player.selectedItem]))
            {

				bool canUse = player.channel && player.HasAmmo(player.inventory[player.selectedItem]) && !player.noItems && !player.CCed;
                int weaponAmmo;
                float shootSpeed;
                int weaponDamage;
                float weaponKnockback;
                player.PickAmmo(player.inventory[player.selectedItem],
                    out weaponAmmo,
                    out shootSpeed,
                    out weaponDamage,
                    out weaponKnockback,
                    out weaponAmmo,
                    canUse
                    );
				if (modPlayer.Skill == 0&&modPlayer.StockCount == 0) {
					modPlayer.OffensiveRecovery();
				}
				Projectile.NewProjectileDirect(player.GetSource_ItemUse_WithPotentialAmmo(player.inventory[player.selectedItem], weaponAmmo),
                    player.Center,
                    Projectile.velocity.RotatedByRandom(rot),
                    ModContent.ProjectileType<KroosAlterCrossbow_Arrow>(),
                    weaponDamage,
                    weaponKnockback,
                    player.whoAmI,
                    0,
                    0,
                    Skill
                    );
            }
        }

        private void ShootEffect(Player player)
        {
            for (int j = 0; j < 2; j++)
            {
                float[] rand = 
                    {
                    Main.rand.NextFloat(-45f, -15f),
                    Main.rand.NextFloat(-15f, 15f),
                    Main.rand.NextFloat(15f, 45f)
                    };

                for (int i = 0; i < 3; i++)
                {
                    float angle = rand[i];
                    float angleMagnitude = Math.Abs(angle);

                    float speedFactor = 0.5f + angleMagnitude / 45f * 0.5f;

                    Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.ToRadians(angle)) * speedFactor * Main.rand.NextFloat(0.5f, 1.5f)  * 8;

                    Projectile.NewProjectileDirect(
                        Projectile.GetSource_FromAI(),
                        Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 16,
                        vel,
                        ModContent.ProjectileType<KroosAlterCrossbow_Effect>(),
                        0,
                        0, 
                        player.whoAmI,
                        0,
                        0,
                        Skill
                    );
                }
            }
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            var origin = new Vector2(tex.Width / 2, tex.Height / 2);
            SpriteEffects effects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );
            return false;
        }

    }
}
