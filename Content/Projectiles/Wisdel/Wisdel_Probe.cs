using ArknightsMod.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WisdelItem = ArknightsMod.Content.Items.Weapons.WisdelCannon;

namespace ArknightsMod.Content.Projectiles.Wisdel
{
    public class Wisdel_Probe : ModProjectile
    {
		#region 音效
		/// <summary>
		/// 部署召唤
		/// </summary>
		public static SoundStyle Summon = new(ArknightsMod.SoundPath + "WisdelCannon/WisdelSummon");
		/// <summary>
		/// 普攻发射
		/// </summary>
		public static SoundStyle Shoot = new(ArknightsMod.SoundPath + "WisdelCannon/WisdelShoot");
		/// <summary>
		/// 普攻命中
		/// </summary>
		public static SoundStyle ShootBlast = new(ArknightsMod.SoundPath + "WisdelCannon/WisdelShootBlast");
		/// <summary>
		/// 普攻重新装填
		/// </summary>
		public static SoundStyle ShootReload = new(ArknightsMod.SoundPath + "WisdelCannon/WisdelReload");
		/// <summary>
		/// 技能开启
		/// </summary>
		public static SoundStyle SkillActivate = new(ArknightsMod.SoundPath + "WisdelCannon/SkillActivate");
		/// <summary>
		/// 三技能锁定
		/// </summary>
		public static SoundStyle Aim = new(ArknightsMod.SoundPath + "WisdelCannon/WisdelAim");
		/// <summary>
		/// 三技能爆破
		/// </summary>
		public static SoundStyle Explode = new(ArknightsMod.SoundPath + "WisdelCannon/WisdelBoom");
		#endregion
		public override string Texture => ArknightsMod.noTexture;
		public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.hide = false;
            Projectile.tileCollide = false;
        }
        public override bool? CanDamage() => false;

		/// <summary>
		/// Style：浮游炮的种类，决定贴图以及方位
		/// </summary>
		public ref float Style {
			get {
				return ref Projectile.ai[0];
			}
		}
		/// <summary>
		/// 组合后目标位置
		/// </summary>
		public Vector2 destiniedPosition;
		public override void OnSpawn(IEntitySource source) {
			Player player = Main.player[Projectile.owner];
			player.wisdel().currentUse = 0;
			player.wisdel().channelTimer = 0;
		}
		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
			Player player = Main.player[Projectile.owner];
			if (player.controlUseItem) {
				overPlayers.Add(index);
			}
		}
		public float Rotation;
		public Vector2 Position;

		public override void AI()
        {
			// 基础设置
            Player player = Main.player[Projectile.owner];
            //player.heldProj = Projectile.whoAmI;
            if (!player.active || player.dead || player.ghost)
            {
                Projectile.Kill();
            }

			if (player.HeldItem.ModItem is WisdelItem)
			{
                Projectile.timeLeft = 2;
            }

            Projectile.spriteDirection = Projectile.direction;
            //player.ChangeDir(Projectile.direction);
            
			if (Main.myPlayer == Projectile.owner)
			{
				if (player.controlUseItem && player.wisdel().currentUse == Style && player.wisdel().coolDown == 0) {
					Vector2 vec = (Main.MouseWorld - Projectile.Center)
								.SafeNormalize(default);
					Projectile.velocity = vec;

					Rotation = MathF.Atan2(vec.Y * Projectile.direction,
						vec.X * Projectile.direction);
				}
				else {
					float rot = Style switch {
						0 => MathHelper.PiOver4,
						3 => MathHelper.PiOver4 * 3,
						2 => MathHelper.PiOver4,
						1 => -MathHelper.PiOver4
					};
					Vector2 dire = Style switch {
						0 => new Vector2(1, -1),
						3 => new Vector2(-1, 1),
						2 => new Vector2(1, 1),
						1 => new Vector2(-1, -1)
					};
					Rotation = rot + (Projectile.direction == -1 ? MathHelper.Pi : 0);
					float raodong = EaseFunction.SineEase((float)Main.timeForVisualEffects + Style * MathHelper.PiOver4, -20f, 20f, 1f) / Main.rand.NextFloat(25,75);
					Projectile.velocity = (Projectile.velocity * 20 + player.velocity.SafeNormalize(default)) / 21f;
					Projectile.Center += dire * raodong;
				}
				Projectile.netUpdate = true;
			}
			if (player.wisdel().mode == 0) {
				int style = Style switch {
					0 => 0,
					3 => 1,
					2 => 2,
					1 => 3
				};
				Position = player.RotatedRelativePoint(player.MountedCenter
					+ new Vector2(Style == 2 ? 30 : 40, 0).RotatedBy(MathHelper.PiOver4 + style * MathHelper.PiOver2)
					+ new Vector2(-10 * Projectile.spriteDirection, 0).RotatedBy(Rotation));
			}
			else if(player.wisdel().mode == 1) {
				Vector2 pos = Style switch {
					0 =>new Vector2(0,0),
					1 => new Vector2(-12,10),
					2 => new Vector2(-46,0),
					3 => new Vector2(-16,-6)
				};
				Position = player.RotatedRelativePoint(player.MountedCenter + pos
					+ new Vector2(-10 * Projectile.spriteDirection, 0).RotatedBy(Rotation));
			}
			Projectile.damage = player.GetWeaponDamage(player.HeldItem);

			//玩家手臂设置
			//player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2 * player.direction);


			if (player.wisdel().coolDown > 0)
            {
                player.itemTime = player.itemAnimation = 2;
                Projectile.netUpdate = true;
				player.wisdel().channelTimer = 0;
                /*if (Main.netMode != NetmodeID.Server)
                {
                    int offset = (int)PHelper.Map(Projectile.ai[1], 0, 40, 1, 15);
                    offset = Math.Clamp(offset, 1, 15);
                    for (int i = 0; i < offset; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(
                            Projectile.Center+ new Vector2(0,8) + Projectile.velocity.SafeNormalize(default) * Projectile.width*0.82f + Projectile.velocity.SafeNormalize(default).RotatedBy(-MathHelper.PiOver2 * player.direction) * 4, 
                            DustID.Smoke, -Vector2.UnitY * 3 * Main.rand.NextFloat(), 200, Color.White, Main.rand.NextFloat(1.5f));
                        dust.noGravity = true;
                    }
                }*/
            }

			/*
            else if (player.controlUseTile || (Projectile.ai[0] > 0 && (Projectile.ai[2] <= 0)))
            {
                player.itemTime = player.itemAnimation = 2;
                if (player.controlUseTile || Projectile.ai[2] <= -1f) // 三技能
                {
                    Projectile.ai[2] -= 0.05f; // 触发三技能条件
                    if (Projectile.ai[2] <= -1f)
                    {
                        player.fullRotation = 0;
                        if (Projectile.ai[0]++ > player.HeldItem.useAnimation * player.GetWeaponAttackSpeed(player.HeldItem))
                        {
                            player.velocity = -Projectile.velocity.SafeNormalize(default) * 15;
                            Projectile.ai[0] = 0;
                            if (player.PickAmmo(player.HeldItem, out var projToShoot, out var speed, out var dmg, out var kn, out var usedAmmoItemId))
                            {
                                Projectile.ai[1] = 180;
                                Projectile.netUpdate = true;
                                if (Main.netMode != NetmodeID.Server)
                                {
                                    SoundEngine.PlaySound(ShootSP, Projectile.position);
                                    for (int i = 0; i < 60; i++)
                                    {
                                        Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(default) * Projectile.width, DustID.FireworksRGB, Projectile.velocity.RotatedByRandom(0.2) * Main.rand.NextFloat(), newColor: Color.White);
                                        dust.noGravity = true;

                                        dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(default) * Projectile.width, DustID.Smoke, Projectile.velocity.RotatedByRandom(0.2) * 0.1f * Main.rand.NextFloat(), 100, Color.White, 2f);
                                        dust.noGravity = true;

                                        Vector2 vel = Projectile.velocity.RotatedBy(i / 60f * MathHelper.TwoPi) * 0.2f;
                                        vel.X *= 0.2f;
                                        dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(default) * Projectile.width, DustID.Smoke, vel, 0, Color.White, 2f);
                                        dust.noGravity = true;
                                        dust.velocity = dust.velocity.RotatedBy(Projectile.rotation);
                                    }
                                }
                                Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ProjectileID.BulletHighVelocity, (int)(Projectile.damage * 1.5f + dmg), (Projectile.knockBack + kn) * 2.5f, Projectile.owner).penetrate = -1;
                            }
                            if (Main.myPlayer == Projectile.owner)
                                Projectile.velocity = (Projectile.velocity * 10 + (Main.MouseWorld - player.Center).SafeNormalize(default).RotatedBy(-MathHelper.PiOver2 * 1.75f * player.direction) * 300) / 11f;
                        }
                        else if ((int)Projectile.ai[0] == (int)(player.HeldItem.useAnimation * player.GetWeaponAttackSpeed(player.HeldItem)) / 3)
                        {
                            if (Main.netMode != NetmodeID.Server)
                                SoundEngine.PlaySound(ChargeSP, Projectile.position);
                        }
                    }
                }
                if (Projectile.ai[2] >= -1f) // 二技能
                {
                    if (Projectile.ai[0]++ > player.HeldItem.useAnimation * player.GetWeaponAttackSpeed(player.HeldItem))
                    {
                        player.velocity = -Projectile.velocity.SafeNormalize(default) * 10;
                        Projectile.ai[0] = 0;
                        if (player.PickAmmo(player.HeldItem, out var projToShoot, out var speed, out var dmg, out var kn, out var usedAmmoItemId))
                        {
                            Projectile.ai[1] = 120;
                            Projectile.netUpdate = true;
                            if (Main.netMode != NetmodeID.Server)
                            {
                                SoundEngine.PlaySound(Shoot, Projectile.position);
                                for (int i = 0; i < 15; i++)
                                {
                                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(default) * Projectile.width, DustID.FireworksRGB, Projectile.velocity.RotatedByRandom(0.2) * Main.rand.NextFloat(), newColor: Main.rand.NextBool(3) ? new Color(235, 65, 26) : new Color(255, 163, 71));
                                    dust.noGravity = true;

                                    dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(default) * Projectile.width, DustID.Smoke, Projectile.velocity.RotatedByRandom(0.2) * 0.1f * Main.rand.NextFloat(), 100, Color.White, 2f);
                                    dust.noGravity = true;
                                }
                            }
                            Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ProjectileID.BulletHighVelocity, Projectile.damage + dmg, (Projectile.knockBack + kn) * 1.5f, Projectile.owner).penetrate = -1;
                        }
                        if (Main.myPlayer == Projectile.owner)
                            Projectile.velocity = (Projectile.velocity * 10 + (Main.MouseWorld - player.Center).SafeNormalize(default).RotatedBy(-MathHelper.PiOver2 * 1.75f * player.direction) * 100) / 11f;
                    }
                    else if ((int)Projectile.ai[0] == (int)(player.HeldItem.useAnimation * player.GetWeaponAttackSpeed(player.HeldItem)) / 3)
                    {
                        if (Main.netMode != NetmodeID.Server)
                            SoundEngine.PlaySound(Charge, Projectile.position);
                    }
                }
            }*/

			if (player.controlUseItem )
            {
				int dir = Main.MouseWorld.X - player.Center.X > 0 ? 1 : -1;
				
				player.ChangeDir(dir);
				if (player.wisdel().currentUse == Style) {
					if (player.wisdel().coolDown == 0) {
						player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2 * player.direction);
					}
					else {
						player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.PiOver4 * player.direction);
					}
					player.wisdel().channelTimer++;

					if (player.wisdel().channelTimer > player.HeldItem.useAnimation * player.GetWeaponAttackSpeed(player.HeldItem) / 3 &&
						player.wisdel().coolDown <= 0) {
						Main.NewText(player.wisdel().currentUse);
						Projectile.netUpdate = true;

						if (Main.netMode != NetmodeID.Server) {
							SoundEngine.PlaySound(Shoot, Projectile.position);

							/*for (int i = 0; i < 15; i++) {
								Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(default) * Projectile.width, DustID.FireworksRGB, Projectile.velocity.RotatedByRandom(0.2) * Main.rand.NextFloat(), newColor: Main.rand.NextBool(3) ? new Color(235, 65, 26) : new Color(255, 163, 71));
								dust.noGravity = true;

								dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity.SafeNormalize(default) * Projectile.width, DustID.Smoke, Projectile.velocity.RotatedByRandom(0.2) * 0.1f * Main.rand.NextFloat(), 100, Color.White, 2f);
								dust.noGravity = true;
							}*/
						}

						int p = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (Main.MouseWorld - Projectile.Center)
								.SafeNormalize(default) * 16,
							ProjectileID.BulletHighVelocity, Projectile.damage, Projectile.knockBack,
							Projectile.owner);
						Main.projectile[p].tileCollide = false;

						player.wisdel().coolDown = 30;

						player.wisdel().currentUse++;
						if (player.wisdel().currentUse > 3)
							player.wisdel().currentUse = 0;
						return;
					}
				}
            }
			else if(player.controlUseTile) {
				if (player.wisdel().coolDown == 0) {
					player.wisdel().mode++;
					if (player.wisdel().mode > 1) { player.wisdel().mode = 0; }
					Main.NewText($"Mode:{player.wisdel().mode}");
					player.wisdel().coolDown = 30;
				}
			}

			Projectile.Center = new Vector2(MathHelper.Lerp(Projectile.Center.X, Position.X, 0.2f),
											MathHelper.Lerp(Projectile.Center.Y, Position.Y, 0.2f));
			Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Rotation, 0.1f);
		}

		/// <summary>
		/// 范围内寻敌
		/// </summary>
		/// <param name="position">寻敌起点</param>
		/// <param name="maxRange">最大距离</param>
		/// <param name="checkCanHit">检查是否有效</param>
		/// <returns></returns>
        public NPC FindTargetWithinRange(Vector2 position, float maxRange, bool checkCanHit = false)
        {
            NPC result = null;
            float num = maxRange;
            for (int i = 0; i < 200; i++)
            {
                NPC nPC = Main.npc[i];
                if (nPC.CanBeChasedBy(this) && Projectile.localNPCImmunity[i] == 0 && (!checkCanHit || Collision.CanHitLine(position, Projectile.width, Projectile.height, nPC.position, nPC.width, nPC.height)))
                {
                    float num2 = Vector2.Distance(position, nPC.Center);
                    if (!(num <= num2))
                    {
                        num = num2;
                        result = nPC;
                    }
                }
            }

            return result;
        }

        public override bool PreDraw(ref Color lightColor) {

			Player player = Main.player[Projectile.owner];
			Texture2D texture = ModContent.Request<Texture2D>($"ArknightsMod/Content/Projectiles/Wisdel/Wisdel_Probe{Style}").Value;
			Texture2D glowTex = ModContent.Request<Texture2D>($"ArknightsMod/Content/Projectiles/Wisdel/Wisdel_Probe_Glow{Style}").Value;
			Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(texture, drawPos, null, lightColor, Projectile.rotation, texture.Size() * 0.5f,
				1f, Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
			Main.EntitySpriteDraw(glowTex, drawPos, null, Color.White, Projectile.rotation, texture.Size() * 0.5f,
			   1f, Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);

			return false;
        }
    }
}
