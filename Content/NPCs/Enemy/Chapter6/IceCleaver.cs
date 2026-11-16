using ArknightsMod.Content.Items.Material;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;


namespace ArknightsMod.Content.NPCs.Enemy.Chapter6
{
	public partial class IceCleaver:global::ArknightsMod.Common.Networking.LegacyEnemyNetwork
	{
		private int fadeTimer;
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 21;
			NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() { // Influences how the NPC looks in the Bestiary
				Velocity = -1f // Draws the NPC in the bestiary as if its walking +1 tiles in the x direction
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}
		public override void SetDefaults() {
			NPC.width = 35;
			NPC.height = 60;
			NPC.damage = 38;
			NPC.defense = 50;
			NPC.lifeMax = 1600;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath3;
			NPC.value = 100f;
			NPC.knockBackResist = 0.20f;
			NPC.aiStyle = -1;
			NPC.scale = 1f;
		}
		public override void OnSpawn(IEntitySource source) {
            if (!NPC.HasValidTarget) NPC.TargetClosest(false);
			fadeTimer = 60; // 持续60帧
			NPC.color = Color.White; // Opacity is advanced by AI, including on servers.
			NPC.alpha = 240;
		}
        public override void FindFrame(int frameHeight) {
            if (frameHeight <= 0 || NPC.GetGlobalNPC<global::ArknightsMod.Content.NPCs.Friendly.CannotSupportNPC>().IsSleeping) return;
            if (attack) NPC.frame.Y = Math.Clamp(AttackCD / 7, 0, 9) * frameHeight;
            else if (Math.Abs(NPC.velocity.X) > .05f) {
                framecounter = (framecounter + 1) % 77;
                NPC.frame.Y = (10 + framecounter / 7) * frameHeight;
            }
            else NPC.frame.Y = 10 * frameHeight;
        }
		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
			NPC.spriteDirection = -NPC.direction;
			Color drawcolor = Color.Lerp(new Color(0, 0, 0, 65), new Color(255, 255, 255, 255), 1f - fadeTimer / 60f);
			// 动态计算原点（水平居中，底部对齐碰撞箱）
			Vector2 origin1 = new Vector2(NPC.frame.Width *2/ 3f, NPC.frame.Height - 55);
			Vector2 origin2 = new Vector2(NPC.frame.Width / 3f, NPC.frame.Height - 55);

			if (NPC.spriteDirection > 0) {
				spriteBatch.Draw(
				texture,
				NPC.Center - screenPos + new Vector2(0, 4f), // 整体下移4像素
				NPC.frame,
				drawcolor,
				NPC.rotation,
				origin1,
				NPC.scale,
				NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
				0f
				);
				
			}
			if (NPC.spriteDirection < 0) {
				spriteBatch.Draw(
				texture,
				NPC.Center - screenPos + new Vector2(0, 4f), // 整体下移4像素
				NPC.frame,
				drawcolor,
				NPC.rotation,
				origin2,
				NPC.scale,
				NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
				0f
				);
				
			}
			return false;
		}

		private int AttackCD = 0;
		private bool attack;
		private bool walk = true;
		private int framecounter;
		private float maxspeed = 1.2f;

		/// <summary>
		/// 陆地npc跳跃 下平台 逻辑 //BY KZ
		/// </summary>
		/// <param name="npc"></param>
		/// <param name="width">npc宽</param>
		/// <param name="height">npc高</param>
		/// <param name="jumpHeight">跳跃高度</param>
		/// <param name="GetOffPlatform">能否下平台</param>
        public static void LandNPCMovementLogic(NPC npc, float width = 0, float height = 0, float jumpHeight = 8, bool GetOffPlatform = true, bool IfJump = true) {
            if (!npc.HasValidTarget) npc.TargetClosest();
            if (!npc.HasValidTarget) return;
            Player player = Main.player[npc.target];
            int ahead = (int)((npc.direction > 0 ? npc.Right.X + 5 : npc.Left.X - 5) / 16);
            int feet = (int)((npc.Bottom.Y - 2) / 16);
            bool Solid(int x, int y) {
                if (!WorldGen.InWorld(x, y, 2)) return true;
                Tile tile = Main.tile[x, y];
                return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
            }
            if (!IfJump || Math.Abs(npc.velocity.Y) > .05f) return;
            int wall = 0;
            for (int i = 0; i < Math.Clamp((int)jumpHeight, 1, 10); i++) {
                if (Solid(ahead, feet - i)) wall = i + 1;
            }
            bool playerAbove = player.Bottom.Y < npc.Top.Y - 24 && Math.Abs(player.Center.X - npc.Center.X) < 180;
            bool hasHeadroom = !Collision.SolidCollision(npc.position - new Vector2(0, 16), npc.width, 14);
            if (hasHeadroom && (wall > 0 || playerAbove || npc.collideX)) {
                // Full hitbox and safe tile reads replace width/16 truncation and
                // the old hard-coded '+3' platform sample shared by snow troops.
                npc.velocity.Y = -Math.Clamp(4 * MathF.Sqrt(Math.Max(wall + 1, playerAbove ? 5 : 2)), 5, 11.3f);
                npc.velocity.X = npc.direction * Math.Max(1.2f, Math.Abs(npc.velocity.X));
                npc.netUpdate = true;
            }
        }

        public override void AI() {
            if (!NPC.HasValidTarget) NPC.TargetClosest();
            if (!NPC.HasValidTarget) return;
            if (!attack) NPC.TargetClosest();
            if (fadeTimer > 0) fadeTimer--;
            NPC.alpha = (int)(240f * fadeTimer / 60f);
            NPC.color = Color.White;
            Player p = Main.player[NPC.target];
			if (walk == true) {
				NPC.spriteDirection = -NPC.direction;
				AttackCD++;
				/*if (NPC.position.X - p.position.X < -100 || (NPC.position.X - p.position.X < 100 && NPC.position.X - p.position.X > 0)) {
					if (NPC.velocity.X < maxspeed) {
						NPC.velocity.X += 0.4f;
					}
					if (NPC.velocity.X >= maxspeed) {
						NPC.velocity.X = maxspeed;
					}
				}

				if (NPC.position.X - p.position.X > 100 || (NPC.position.X - p.position.X > -100 && NPC.position.X - p.position.X < 0)) {
					if (NPC.velocity.X > -maxspeed) {
						NPC.velocity.X -= 0.4f;
					}
					if (NPC.velocity.X <= -maxspeed) {
						NPC.velocity.X = -maxspeed;
					}
				}*/

				//26.6.10 改
				//优化移动逻辑
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * maxspeed, 0.1f);


				/*
				if (Math.Abs(NPC.velocity.X) <= 0.5f) {
					jumpCD++;
				}
				if (jumpCD >= 180) {
					jumpCD = 0;
					NPC.velocity.Y = -7.2f;
				}
				*/


				//26.6.10 改
				//防止神秘太空步
				if (Math.Abs(NPC.position.X - p.position.X) <= 100 && Math.Abs(NPC.position.Y - p.position.Y) <= 100) {

					NPC.velocity.X = 0;
					if (AttackCD >= 100 && !attack) {
						walk = false;
						attack = true;
						AttackCD = 0;
					}
				}
				if(Math.Abs(NPC.position.X - p.position.X) >= 120) {
					AttackCD = 114514;
				}

				//26.6.10 改
				//跳+下平台
				LandNPCMovementLogic(NPC, NPC.width, NPC.height, 8);

			}
			if (attack == true) {
				NPC.velocity.X = 0;
				AttackCD++;
				NPC.damage = 76;
				if (AttackCD == 25) {
					if (NPC.spriteDirection < 0) {
						global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X + 60, NPC.Center.Y + 40), new Vector2(0, 0), ModContent.ProjectileType<Icebreak>(), 38, 0.8f);

					}
					if (NPC.spriteDirection > 0) {
						global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(NPC.GetSource_FromThis(), new Vector2(NPC.Center.X - 60, NPC.Center.Y + 40), new Vector2(0, 0), ModContent.ProjectileType<Icebreak>(), 38, 0.8f);

					}
				}
				
				if (AttackCD > 70) {
					attack = false;
					walk = true;
					AttackCD = 0;
					NPC.damage = 38;
				}
			}
		}

		public override bool? CanFallThroughPlatforms() {
			Player player = Main.player[NPC.target];
			return (player.position.Y + player.height) - (NPC.position.Y + NPC.height) > 0;
		}
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			Player p = Main.player[NPC.target];
			if (p.frozen == true) {
				modifiers.SourceDamage *= 3;

			}
			if (Main.expertMode)
				modifiers.SourceDamage *= 1.5f; // 专家模式伤害 ×1.5
			if (Main.masterMode)
				modifiers.SourceDamage *= 2f;   // 大师模式伤害 ×2
		}
		public override void ModifyNPCLoot(NPCLoot npcLoot) {

			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Device>(), 8, 1, 1));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Polyketon>(), 8, 1, 1));

		}
	}

	public partial class Icebreak : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetDefaults() {
			Projectile.width = 180;
			Projectile.height = 180;
			Projectile.damage = 76;
			Projectile.penetrate = 9999;
			Projectile.tileCollide = false;
			Projectile.hostile = true;
		}
		private int flytime;
		public override void AI() { 
			flytime++;
			if (flytime >= 50) {
				Projectile.timeLeft = 1;
			}
			Dust dust;
			dust = Dust.NewDustDirect(Projectile.position,135, 135, DustID.Ice, 0, 0, 0, default, 1);
			dust.noGravity = true;
		}
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			if (target.frozen == true) {
				modifiers.SourceDamage *= 3;
			}
			if (Main.expertMode)
				modifiers.SourceDamage *= 0.75f; // 专家模式伤害 ×1.5
			else if (Main.masterMode)
				modifiers.SourceDamage *= 0.7f;   // 大师模式伤害 ×2
		}
	}
}
