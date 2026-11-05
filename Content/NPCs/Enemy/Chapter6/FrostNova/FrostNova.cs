using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.BossBars;
using ArknightsMod.Content.Items.Material;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova
{
	[AutoloadBossHead]
	public partial class FrostNova : global::ArknightsMod.Common.Networking.LegacyEnemyNetwork
	{
		#region 基本属性设置
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 76;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers() {
				CustomTexturePath = "ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/FrostNova_preview",
				PortraitScale = 1f,
				PortraitPositionYOverride = 0f,
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
		}

		public override void SetDefaults() {
			NPC.width = 40;
			NPC.height = 58;
			NPC.damage = 0;
			NPC.defense = 10;
			NPC.lifeMax = 20000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.DoubleJump;
			NPC.knockBackResist = 0f;
			NPC.lavaImmune = true;
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			NPC.value = Item.buyPrice(gold: 5);
			NPC.boss = true;
			NPC.npcSlots = 10f;
			NPC.aiStyle = -1;
			NPC.BossBar = ModContent.GetInstance<NoBossBar>();
			NPC.dontTakeDamage = true;
			NPC.Opacity = 0f;
		}

		//修改血量不生效，因为出场无敌
		//public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) {
		//	NPC.lifeMax = (int)(NPC.lifeMax * 0.75f * balance);
		//	NPC.damage = (int)(NPC.damage * 0.8f);
		//}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,

				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.ArknightsMod.Bestiary.FrostNova"))
			});
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ItemType<IncandescentAlloyBlock>(), 3, 3, 5));
			npcLoot.Add(ItemDropRule.Common(ItemType<CrystallineCircuit>(), 3, 3, 5));
			npcLoot.Add(ItemDropRule.Common(ItemType<OptimizedDevice>(), 3, 3, 5));
			// 霜星掉落：12 源石锭 & 1000 合成玉
			npcLoot.Add(ItemDropRule.Common(ItemType<global::ArknightsMod.Content.Items.OriginiumIngot>(), 1, 12, 12));
			npcLoot.Add(ItemDropRule.Common(ItemType<global::ArknightsMod.Content.Items.Orundum>(), 1, 1000, 1000));
		}
		#endregion
		#region 自定义血条
		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) {
			return false;
		}

		private float Bartimer;
		private float TargetHealthBarLength;
		private float ActualHealthBarLength;
		//自制血条
		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D BarTexBot = ModContent.Request<Texture2D>("ArknightsMod/Content/BossBars/FNBossBarBot").Value;
			Texture2D BarTexMed2 = ModContent.Request<Texture2D>("ArknightsMod/Content/BossBars/FNBossBarMed2").Value;
			Texture2D BarTexMed = ModContent.Request<Texture2D>("ArknightsMod/Content/BossBars/FNBossBarMed").Value;
			Texture2D BarTexTop = ModContent.Request<Texture2D>("ArknightsMod/Content/BossBars/FNBossBarTop").Value;
			if (!FNDeathStart) {
				Bartimer++;
				if (Bartimer > 120) {
					Bartimer = 120;
				}
			}
			else {
				Bartimer--;
				if (Bartimer < 0) {
					Bartimer = 0;
				}
			}
			TargetHealthBarLength = BarTexMed.Width * NPC.life / NPC.lifeMax * Bartimer / 120;
			if (ActualHealthBarLength > TargetHealthBarLength / 4) {
				ActualHealthBarLength--;
			}
			if (ActualHealthBarLength < TargetHealthBarLength / 4) {
				ActualHealthBarLength++;
			}
			float fixedscr = Main.screenWidth > 1920 ? ((Main.screenWidth - 1920) / 1920f * 2.75f + 1) * (Main.screenHeight > 1080 ? Main.screenHeight / 1080f : 1) : 1;
			Main.EntitySpriteDraw(BarTexBot, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2 + (Main.screenHeight / 2 - 108 * fixedscr) / Main.GameZoomTarget), new Rectangle(0, 0, (int)(BarTexBot.Width), BarTexBot.Height), Color.White * (Bartimer / 180), 0, new Vector2(BarTexBot.Width / 2, BarTexBot.Height / 2), 1 / Main.GameZoomTarget, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(BarTexMed2, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2 + (Main.screenHeight / 2 - 108 * fixedscr) / Main.GameZoomTarget), new Rectangle(0, 0, (int)(ActualHealthBarLength * 4), BarTexMed.Height), Color.White * (Bartimer / 120), 0, new Vector2(BarTexMed.Width / 2, BarTexMed.Height / 2), 1 / Main.GameZoomTarget, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(BarTexMed, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2 + (Main.screenHeight / 2 - 108 * fixedscr) / Main.GameZoomTarget), new Rectangle(0, 0, (int)(TargetHealthBarLength), BarTexMed.Height), Color.White * (Bartimer / 120), 0, new Vector2(BarTexMed.Width / 2, BarTexMed.Height / 2), 1 / Main.GameZoomTarget, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(BarTexTop, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2 + (Main.screenHeight / 2 - 108 * fixedscr) / Main.GameZoomTarget), new Rectangle(0, 0, (int)(BarTexTop.Width), BarTexTop.Height), Color.White * (Bartimer / 30), 0, new Vector2(BarTexTop.Width / 2, BarTexTop.Height / 2), 1 / Main.GameZoomTarget, SpriteEffects.None, 0);
		}
		#endregion
        // Lifecycle and animation clocks shared by the encounter scheduler and sprite sheet.
        private float BirthTimer, ReBirthTimer, escapetimer, DeathTimer, FNShaderRingTimer;
        private float FNStage, FNATKTimer, FNSkillTimer;
        private bool FNBirth, FNBirthEnd, FNRevival, FNRevivalStart, FNRevivalEnd, FNEnd, FNDeathStart;
        private bool FNOnATK, FNOnSkill, FNATKFrameInitialized, FNSkillFrameInitialized;

		public override void AI() => RunReworkedAI();
		#region 死亡检查机制
		public override bool CheckDead() {
			RetireEncounter();
			ResetCombat();
			NPC.netUpdate = true;
			if (!FNRevival) {
				NPC.dontTakeDamage = true;
				DeathTimer = 0;
				NPC.life = 1;
				FNStage = 1.5f;
				FNRevivalStart = true;
				return false;
			}
			else if (!FNEnd) {
				NPC.dontTakeDamage = true;
				DeathTimer = 0;
				NPC.life = 1;
				NPC.damage = 0;
				FNStage = 3;
				FNDeathStart = true;
				return false;
			}
			return true;
		}

		public override void OnKill() {
			RetireEncounter();
			if (Main.netMode != NetmodeID.Server && Terraria.Graphics.Effects.Filters.Scene["FNTwistedRing"].IsActive()) {
				Terraria.Graphics.Effects.Filters.Scene["FNTwistedRing"].Deactivate();
			}
		}
		#endregion
		#region 帧图绘制
		//帧动画，霜星一共76帧，1~5为走路，6~16为普攻，17~34为坠冰技能，35~55为重生（35~39为死亡），56~76为吟唱
		public override void FindFrame(int frameHeight) {
			int startFrame;
			int finalFrame;
			int frameSpeed;
			NPC.spriteDirection = NPC.direction;
            if (AirbornePhase && ReBirthTimer > FrostNovaCombatRules.PhaseTwoEntrance && !FNOnATK && !FNOnSkill) {
                NPC.frame.Y = 55 * frameHeight;
                return;
            }
			//走路
			if ((FNStage == 1 || (FNStage == 2 && ReBirthTimer > FrostNovaCombatRules.PhaseTwoEntrance) || FNRevivalEnd) && !FNOnATK && !FNOnSkill) {
				startFrame = 0;
				finalFrame = 4;
				frameSpeed = 5;
				//if (State == ActionState.FastWalk) {
				//	frameSpeed = 4;
				//}
				NPC.frameCounter += 0.5f;
				if (NPC.frameCounter > frameSpeed) {
					NPC.frameCounter = 0;
					if (Math.Abs(NPC.velocity.X) > .1f) { // 距离过近时站着不动
						NPC.frame.Y += frameHeight;
					}

					if (NPC.frame.Y > finalFrame * frameHeight) {
						NPC.frame.Y = startFrame * frameHeight;
					}
				}
			}
			//普通攻击
			if ((FNStage == 1 || (FNStage == 2 && ReBirthTimer > FrostNovaCombatRules.PhaseTwoEntrance) || FNRevivalEnd) && FNOnATK && FNATKTimer >= 156) {
				startFrame = 5;
				finalFrame = 15;
				frameSpeed = 6;
				if (!FNATKFrameInitialized) {
					NPC.frame.Y = startFrame * frameHeight;
					FNATKFrameInitialized = true;
				}
				else {
					NPC.frameCounter++;
					if (NPC.frame.Y < finalFrame * frameHeight) {
						if (NPC.frameCounter > frameSpeed) {
							NPC.frameCounter = 0;
							NPC.frame.Y += frameHeight;
						}
					}
				}
			}
			//技能
			if ((FNStage == 1 || (FNStage == 2 && ReBirthTimer > FrostNovaCombatRules.PhaseTwoEntrance) || FNRevivalEnd) && FNOnSkill && FNSkillTimer >= 480) {
				startFrame = 16;
				finalFrame = 33;
				frameSpeed = 12;
				if (!FNSkillFrameInitialized) {
					NPC.frame.Y = startFrame * frameHeight;
					FNSkillFrameInitialized = true;
				}
				else {
					NPC.frameCounter++;
					if (FNSkillTimer <= 804 && NPC.frame.Y > 25 * frameHeight) {
						NPC.frame.Y = 18 * frameHeight;
					}
					else if (FNSkillTimer > 804 && FNSkillTimer < 888) {
						NPC.frame.Y = 25 * frameHeight;
					}
					else if (FNSkillTimer == 888) {
						NPC.frame.Y = 26 * frameHeight;
					}
					if (NPC.frame.Y < finalFrame * frameHeight) {
						if (NPC.frameCounter > frameSpeed && NPC.frame.Y != 25 * frameHeight) {
							NPC.frameCounter = 0;
							NPC.frame.Y += frameHeight;
						}
					}
				}
			}
			//重生
			if (FNStage == 1.5f) {
				startFrame = 34;
				finalFrame = 54;
				if (NPC.frame.Y < startFrame * frameHeight) {
					NPC.frame.Y = startFrame * frameHeight;
				}
				if (NPC.frame.Y == 42 * frameHeight) { //在地上多躺一会
					frameSpeed = 180;
					if (NPC.frameCounter == 144) {
						SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNRevive1") with { Volume = 0.8f, Pitch = 0f }, NPC.Center);
					}
				}
				else {
					frameSpeed = 15;
				}
				if (NPC.frame.Y == 51 * frameHeight && NPC.frameCounter == 0) {
					SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNRevive2") with { Volume = 1.2f, Pitch = 0f }, NPC.Center);
				}
				NPC.frameCounter++;
				if (NPC.frameCounter > frameSpeed) {
					NPC.frameCounter = 0;
					NPC.frame.Y += frameHeight;

					if (NPC.frame.Y > finalFrame * frameHeight) {
						NPC.frame.Y = finalFrame * frameHeight;
						FNRevivalEnd = true;
						NPC.dontTakeDamage = true;
					}
				}
			}
			//死亡
			if (FNDeathStart) {
				startFrame = 34;
				finalFrame = 38;
				if (NPC.frame.Y < startFrame * frameHeight) {
					NPC.frame.Y = startFrame * frameHeight;
				}

				frameSpeed = 10;
				NPC.frameCounter += 0.5f;
				if (NPC.frameCounter > frameSpeed) {
					NPC.frameCounter = 0;
					NPC.frame.Y += frameHeight;

					if (NPC.frame.Y > finalFrame * frameHeight) {
						NPC.frame.Y = finalFrame * frameHeight;
					}
				}
			}
			//出场吟唱
			if (BirthTimer < 240 || (FNStage == 2 && ReBirthTimer < FrostNovaCombatRules.PhaseTwoEntrance)) {
				startFrame = 55;
				finalFrame = 75;
				frameSpeed = 7;
				if (!FNBirth || (FNStage == 2 && ReBirthTimer < FrostNovaCombatRules.PhaseTwoEntrance)) {
					NPC.frameCounter++;
					//如果超出范围就锁定第一帧
					if (NPC.frame.Y == finalFrame * frameHeight) {
						if (NPC.frameCounter > frameSpeed) {
							FNBirth = true;
						}
					}
					else {//从第56帧开始
						if (NPC.frame.Y < startFrame * frameHeight) {
							NPC.frame.Y = startFrame * frameHeight;
						}
						if (NPC.frameCounter > frameSpeed) {
							NPC.frameCounter = 0;
							NPC.frame.Y += frameHeight;
						}
					}
				}
				else {
					NPC.frameCounter = 0;
					NPC.frame.Y = 0;
				}
			}
		}
		#endregion
	}

	public partial class FrostNovaJump : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetDefaults() {
			Projectile.width = 80;
			Projectile.height = 80;
			Projectile.aiStyle = 0;
			Projectile.timeLeft = 250;
			Projectile.penetrate = -1;
			Projectile.scale = 0.01f;
			Projectile.Opacity = 0f;
			Projectile.hide = false;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.netUpdate = true;
		}

		public override void AI() {
			Projectile.ai[0] += 1;
			if (Projectile.localAI[0] == 0f) {
				Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
				Projectile.localAI[0] = Main.rand.Next(2) * 2 - 1; // 1 or -1
				Projectile.localAI[1] = Main.rand.NextFloat(Projectile.ai[1], Projectile.ai[1] + 40);
				Projectile.localAI[2] = Main.rand.Next(1, 5);
			}
			//NPC npc = Main.npc[Projectile.owner];
			//float positionX = npc.position.X - 10;
			//float positionY = npc.position.Y;
			//if (Projectile.ai[1] > 0f) {
			//	Projectile.velocity.X = Math.Max(Projectile.velocity.X - 0.1f, 0f);
			//}
			//else {
			//	Projectile.velocity.X = Math.Min(Projectile.velocity.X + 0.1f, 0f);
			//}
			//if (Projectile.ai[2] > 0f) {
			//	Projectile.velocity.Y = Math.Max(Projectile.velocity.Y - 0.1f, 0f);
			//}
			//else {
			//	Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.1f, 0f);
			//}

			//Projectile.velocity.X = GetSPV(Projectile.ai[1], Projectile.ai[1] + 20, Projectile.velocity.X, 2);
			//Projectile.velocity.Y = GetSPV(Projectile.ai[2], Projectile.ai[2] + 20, Projectile.velocity.Y, 2);

			if (Projectile.ai[0] >= 250)
				Projectile.Kill();

			FadeInAndOut();
		}

		public void FadeInAndOut() {
			// If last less than 50 ticks — fade in, than more — fade out
			if (Projectile.ai[0] <= Projectile.localAI[1]) {
				// Fade in
				Projectile.Opacity += 0.1f;
				Projectile.scale += 0.05f;
				Projectile.rotation += 0.003f * Projectile.ai[2];
				Projectile.velocity *= 0.95f;
				// Cap
				if (Projectile.Opacity > 1f)
					Projectile.Opacity = 1f;
				if (Projectile.scale > 1f) {
					Projectile.scale = 1f;
				}
				return;
			}

			// Fade out
			Projectile.Opacity -= 0.03f;
			Projectile.velocity *= 0;
			Projectile.rotation += 0.005f * Projectile.ai[2];
			if (Projectile.Opacity < 0)
				Projectile.Opacity = 0;
			//if (Projectile.localAI[1] <= Projectile.ai[0] && Projectile.ai[0] <= (Projectile.localAI[1] + 60f) && Projectile.ai[0] % 10 == 0 && Projectile.localAI[0] == 1) {
			//	Dust dust = Main.dust[Dust.NewDust(Projectile.Left + new Vector2(-30, -25), 50, Projectile.height / 2, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, -2f)];
			//	dust.noGravity = true;
			//	dust.fadeIn = 0f;
			//	dust.scale = 1.5f;
			//}
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);


			Texture2D texture = Request<Texture2D>("ArknightsMod/Assets/GrayScaleTexture/Smoke" + (int)Projectile.localAI[2], AssetRequestMode.ImmediateLoad).Value;
			float opacity = Projectile.Opacity * 0.6f;
			Color color = Color.White * opacity;
			float scale = Projectile.scale;
			Vector2 origin = texture.Size() * 0.5f;
			//float rotation = 2f * (float)Math.PI * Main.rand.NextFloat();
			Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			return false;
		}
	}
	public class FrostNovaRevival : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 14;
			//DrawOriginOffsetY = -40;
		}
		public override void SetDefaults() {
			Projectile.width = 196;
			Projectile.height = 86;
			Projectile.aiStyle = 0;
			Projectile.timeLeft = 250;
			Projectile.penetrate = -1;
			//Projectile.scale = 0.01f;
			//Projectile.Opacity = 0f;
			Projectile.hide = false;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.netUpdate = true;
		}

		public override void AI() {
			Projectile.direction = (int)Projectile.ai[0];
			Projectile.spriteDirection = Projectile.direction;

			if (++Projectile.frameCounter >= 14) {
				Projectile.frameCounter = 0;
				if (Projectile.frame < 15) {
					Projectile.frame++;
				}
				else {
					Projectile.Kill();
				}
			}
		}
	}
	public partial class FrostNovaSmoke : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetDefaults() {
			Projectile.width = 80;
			Projectile.height = 80;
			Projectile.aiStyle = 0;
			Projectile.timeLeft = 250;
			Projectile.penetrate = -1;
			Projectile.scale = 0.01f;
			Projectile.Opacity = 0f;
			Projectile.hide = false;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.netUpdate = true;
		}

		public override void AI() {
			Projectile.ai[0] += 1;
			if (Projectile.localAI[0] == 0f) {
				Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
				Projectile.localAI[0] = Main.rand.Next(2) * 2 - 1; // 1 or -1
				Projectile.localAI[1] = Main.rand.NextFloat(Projectile.ai[1], Projectile.ai[1] + 40);
				Projectile.localAI[2] = Main.rand.Next(1, 5);
			}
			//NPC npc = Main.npc[Projectile.owner];
			//float positionX = npc.position.X - 10;
			//float positionY = npc.position.Y;
			//if (Projectile.ai[1] > 0f) {
			//	Projectile.velocity.X = Math.Max(Projectile.velocity.X - 0.1f, 0f);
			//}
			//else {
			//	Projectile.velocity.X = Math.Min(Projectile.velocity.X + 0.1f, 0f);
			//}
			//if (Projectile.ai[2] > 0f) {
			//	Projectile.velocity.Y = Math.Max(Projectile.velocity.Y - 0.1f, 0f);
			//}
			//else {
			//	Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.1f, 0f);
			//}

			//Projectile.velocity.X = GetSPV(Projectile.ai[1], Projectile.ai[1] + 20, Projectile.velocity.X, 2);
			//Projectile.velocity.Y = GetSPV(Projectile.ai[2], Projectile.ai[2] + 20, Projectile.velocity.Y, 2);

			if (Projectile.ai[0] >= 250)
				Projectile.Kill();

			FadeInAndOut();
		}

		public void FadeInAndOut() {
			// If last less than 50 ticks — fade in, than more — fade out
			if (Projectile.ai[0] <= Projectile.localAI[1]) {
				// Fade in
				Projectile.Opacity += 0.1f;
				Projectile.scale += 0.1f;
				Projectile.rotation += 0.003f * Projectile.ai[2];
				Projectile.velocity *= 0.95f;
				// Cap
				if (Projectile.Opacity > 1f)
					Projectile.Opacity = 1f;
				if (Projectile.scale > 1f) {
					Projectile.scale = 1f;
				}
				return;
			}

			// Fade out
			Projectile.Opacity -= 0.01f;
			Projectile.velocity *= 0;
			Projectile.rotation += 0.005f * Projectile.ai[2];
			if (Projectile.Opacity < 0)
				Projectile.Opacity = 0;
			if (Projectile.localAI[1] <= Projectile.ai[0] && Projectile.ai[0] <= (Projectile.localAI[1] + 60f) && Projectile.ai[0] % 10 == 0 && Projectile.localAI[0] == 1) {
				Dust dust = Main.dust[Dust.NewDust(Projectile.Left + new Vector2(-30, -25), 50, Projectile.height / 2, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, -2f)];
				dust.noGravity = true;
				dust.fadeIn = 0f;
				dust.scale = 1.5f;
			}
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);


			Texture2D texture = Request<Texture2D>("ArknightsMod/Assets/GrayScaleTexture/Smoke" + (int)Projectile.localAI[2], AssetRequestMode.ImmediateLoad).Value;
			float opacity = Projectile.Opacity * 0.6f;
			Color color = Color.White * opacity;
			float scale = Projectile.scale;
			Vector2 origin = texture.Size() * 0.5f;
			//float rotation = 2f * (float)Math.PI * Main.rand.NextFloat();
			Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			return false;
		}
	}
	public class FrostNovaWhiteRing : ModProjectile
	{
		public override string Texture => "ArknightsMod/Assets/GrayScaleTexture/WhiteRing";
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 12;
			//DrawOriginOffsetY = -40;
		}
		public override void SetDefaults() {
			Projectile.width = 1000;
			Projectile.height = 1000;
			Projectile.aiStyle = 0;
			Projectile.timeLeft = 250;
			Projectile.penetrate = -1;
			Projectile.scale = 2f;
			//Projectile.Opacity = 0f;
			Projectile.hide = false;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.netUpdate = true;
		}

		public override void AI() {
			Projectile.ai[0]++;
			if (Projectile.ai[0] < 60) {
				Projectile.scale -= 0.06f;
				if (Projectile.scale < 0.001f) {
					Projectile.scale = 0.001f;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			Texture2D texture = Request<Texture2D>("ArknightsMod/Assets/GrayScaleTexture/WhiteRing", AssetRequestMode.ImmediateLoad).Value;
			float opacity = Projectile.Opacity * 0.6f;
			Color color = Color.White * opacity;
			float scale = Projectile.scale;
			Vector2 origin = texture.Size() * 0.5f;
			//float rotation = 2f * (float)Math.PI * Main.rand.NextFloat();
			Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			return false;
		}
	}
	//往左右方向抛出的冰球，ai0分别设置为-1和1（由霜星发射时判定），同值传递给Storm
	public partial class BlizzardStormStarter : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 60;
		}

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 6000;
			Projectile.alpha = 0;
			Projectile.damage = 20;
			Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 1f;
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			var newSource = Projectile.GetSource_FromThis();
			global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center - new Vector2(0, 14), Vector2.Zero, ModContent.ProjectileType<BlizzardStorm>(), Projectile.damage, 0f, 0, Projectile.ai[0]);
			SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNSummonIceAltar") with { Volume = 0.8f, Pitch = 0f }, Projectile.Center);
			return true;
		}

		private float timer;
		private int r;
		private int g;
		private float move1X;
		private float move1Y;
		private float oldPosX;
		private float oldPosY;

		//碰上就冻结
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public static int HostNPCType() {
			return ModContent.NPCType<FrostNova>();
		}

		public override void AI() {
			var newSource = Projectile.GetSource_FromThis();
			timer++;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC SeekForNPCs = Main.npc[i];
				if (SeekForNPCs.active && SeekForNPCs.type == HostNPCType()) {
					if (SeekForNPCs.life == 1) {
						Projectile.timeLeft = 0;
					}
					else {
						Projectile.timeLeft = 60;
					}
				}
			}
			r = 235 + 20 * (int)MathF.Sin(timer * MathHelper.Pi / 180);
			g = 245 + 10 * (int)MathF.Sin(timer * MathHelper.Pi / 180);

			if (Main.rand.NextFloat() < 0.25f) {
				Dust.NewDustPerfect(Projectile.Center, DustType<Dusts.Bosses.FrostNovaDeathDust>(), Vector2.Zero, 120, new Color(255, 255, 255), 0.5f);
			}

			float uptimer = 60;
			float spintimer = 500;
			float lerptimer = 30;

			if (timer == uptimer) {
				oldPosX = Projectile.Center.X;
				oldPosY = Projectile.Center.Y;
			}
			if (timer == uptimer + spintimer) {
				Projectile.velocity.X = Projectile.ai[0] * 8f;
				Projectile.velocity.Y = -Projectile.velocity.X;
			}

			if (timer <= uptimer) {
				Projectile.velocity = new Vector2(0, -7f * (1 - timer / uptimer));
			}
			else if (timer <= uptimer + spintimer) {
				float percentOfT = 1 - (timer - uptimer) / (2 * spintimer);
				move1X = Projectile.ai[0] * (timer - uptimer) / 5f * MathF.Sin((timer - uptimer) * MathHelper.Pi / (120 * percentOfT));
				move1Y = Projectile.ai[0] * (timer - uptimer) / 5f * MathF.Cos((timer - uptimer) * MathHelper.Pi / (120 * percentOfT));
				Projectile.Center = new Vector2(move1X + oldPosX, move1Y + oldPosY);//期间的位置变动
			}
			else if (timer <= uptimer + spintimer + lerptimer) {
				Projectile.velocity.Y = float.Lerp(Projectile.velocity.Y, 0, 0.15f);
				Projectile.velocity.X = float.Lerp(Projectile.velocity.X, Projectile.ai[0] * 4f, 0.25f);
			}
			else {
				//Projectile.velocity.Y += 0.03f;
				Projectile.velocity.Y = 0f;
				Projectile.velocity.X = float.Lerp(Projectile.velocity.X, 0, 0.00625f);
				if (Math.Abs(Projectile.velocity.X) <= 0.25f) {
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center - new Vector2(0, 14), Vector2.Zero, ModContent.ProjectileType<BlizzardStorm>(), Projectile.damage, 0f, 0, Projectile.ai[0]);
					SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNSummonIceAltar") with { Volume = 0.8f, Pitch = 0f }, Projectile.Center);
					Projectile.Kill();
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/WindTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color(r, g, 255), new Color(0, 0, 0), 15f * Math.Min(timer / 60, 1), true);
			return true;
		}
	}
	//浮在空中的源石冰晶
	public partial class BlizzardStorm : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/FNIceCrystal";

		public override void SetDefaults() {
			Projectile.width = 44;
			Projectile.height = 52;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 6000;
			Projectile.alpha = 0;
			Projectile.damage = 30;
			Projectile.light = 0.2f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 1f;
			Projectile.rotation = -23 * float.Pi / 180f;
		}

		private readonly int trailnum = Main.rand.Next(8, 13);//随机8-12条轨迹

		public static int HostNPCType() {
			return ModContent.NPCType<FrostNova>();
		}

		//碰上就冻结
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		private float timer;
		private float startY;

		public override void PostDraw(Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D ICGlowT = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/ICGlow").Value;
			Texture2D ICRingT = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/ICRing").Value;
			Main.EntitySpriteDraw(ICGlowT, Projectile.Center - Main.screenPosition + new Vector2(0, 0), new Rectangle(0, 0, ICGlowT.Width, ICGlowT.Height), Color.White * Projectile.Opacity, Projectile.rotation, new Vector2(ICGlowT.Width / 2, ICGlowT.Height / 2), 1f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ICRingT, Projectile.Center - Main.screenPosition + new Vector2(0, 0), new Rectangle(0, 0, ICRingT.Width, ICRingT.Height), Color.White * Projectile.Opacity, 0, new Vector2(ICRingT.Width / 2, ICRingT.Height / 2), 1.2f, SpriteEffects.None, 0);

		}

		public override void OnSpawn(IEntitySource source) {
			startY = Projectile.Center.Y;
		}

		public override void AI() {
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			var newSource = Projectile.GetSource_FromThis();
			timer++;

			Projectile.Center = new Vector2(Projectile.Center.X, startY + 16 * MathF.Sin(timer * float.Pi / 180f));

			if (timer < 30) {
				for (int i = 0; i < 3; i++) {
					Dust.NewDustDirect(new Vector2(Projectile.Center.X, startY + timer * 10), 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
					Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
				}
				Projectile.Opacity = timer / 30f;
			}
			else if (timer == 30) {
				SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/SpawnDevice") with { Volume = 1f, Pitch = 0f }, Projectile.Center);
				Projectile.Opacity = 1;
			}

			if (timer >= 30) {
				if ((int)timer % 60 == 0) {
					for (int i = 0; i < Math.Min(6, trailnum); i++) {//生成轨迹，ai0为相位，2iπ/个数
						global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, new Vector2(Projectile.Center.X, startY + 292), Vector2.Zero, ModContent.ProjectileType<Blizzard>(), Projectile.damage, 0f, 0, 2 * i * MathHelper.Pi / Math.Min(6, trailnum));
					}
				}
			}

			//生成与消失
			if (Projectile.timeLeft > 30) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC SeekForNPCs = Main.npc[i];
					if (SeekForNPCs.active && SeekForNPCs.type == HostNPCType()) {
						if (SeekForNPCs.life == 1) {
							Projectile.timeLeft = 30;
							SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/IceCrystal") with { Volume = 1f, Pitch = 0f }, Projectile.Center);
							for (int j = 0; j < 4; j++) {
								Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
							}
						}
						else {
							Projectile.timeLeft = 60;
						}
					}
				}
			}
			else {
				for (int j = 0; j < 4; j++) {
					Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
				}
				Projectile.Opacity = Projectile.timeLeft / 30f;
			}


		}

		public override void OnKill(int timeLeft) {
			for (int i = 0; i < 4; i++) {
				Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
			}
		}
	}
	//单个的龙卷风团
	public partial class Blizzard : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 60;
		}

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 600;
			Projectile.alpha = 0;
			Projectile.damage = 10;
			Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 1f;
		}

		private float timer;//计时器
		private float addspeedy;//竖直方向的向上加速
		private float ScreenMedPlaceDisRate;//到屏幕中央的距离占比
		private float cosspeedy;//竖直方向的余弦运动，振幅随时间变大，与到屏幕中央的距离成正比
		private float sinspeedx;//水平方向的正弦运动，振幅随时间变大
		private float colorstate;//左右方向颜色不同
		private int r;
		private int g;

		//碰上就冻结
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public override void AI() {
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];

			timer++;
			ScreenMedPlaceDisRate = Math.Abs(2 * (Projectile.Center.Y - Player.Center.Y) / 1080);
			addspeedy = -timer / 90;
			cosspeedy = ScreenMedPlaceDisRate * Math.Min(timer / 240, 1) * (timer / 45) * (float)Math.Cos(timer * MathHelper.Pi / 120 + Projectile.ai[0]);
			sinspeedx = (float)((Math.Pow(1.004f, 1.6f * timer) - 1) * Math.Min(timer / 360, 1) * Math.Sin(timer * MathHelper.Pi / 120 + Projectile.ai[0]));
			Projectile.velocity = new Vector2(sinspeedx, addspeedy + cosspeedy);
			if (Projectile.velocity.X >= 0) {
				colorstate = 1;
			}
			else {
				colorstate = 0.5f;
			}

			r = 235 + 20 * (int)MathF.Sin(timer * MathHelper.Pi / 180);
			g = 245 + 10 * (int)MathF.Sin(timer * MathHelper.Pi / 180);

			if (Main.rand.NextFloat() < Math.Min(timer / 960, 0.25f)) {
				Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
			}
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/WindTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color((int)(r * colorstate), (int)(g * colorstate), (int)(255 * colorstate)), new Color(0, 0, 0), 35f, true);
			return true;
		}
	}
	//冰柱
	public partial class BlackIceProjectile : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 6;
		}
		public override void SetDefaults() {
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.aiStyle = 0;
			Projectile.damage = 10;
			Projectile.timeLeft = 65536;
			Projectile.penetrate = -1;
			Projectile.scale = 1f;
			Projectile.Opacity = 0f;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.netUpdate = true;
		}

		private float timer;
		private readonly Vector2[] oldPos = new Vector2[13]; //残影个数
		private float deltatime;
		private float offsettime;

		public override void PostDraw(Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D Ice = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/BlackIceProjectile").Value;

			if (Projectile.frame == 5) {
				//残影
				for (int i = oldPos.Length - 1; i > 0; i--) {
					if (oldPos[i] != Vector2.Zero && i % 4 == 0) {//1%3是为了增大残影绘制间隔；Color.White * 1 * (1 - m*f * i)是残影淡化； (oldPos[i - 1] - oldPos[i]).ToRotation() + (float)(0.5 * MathHelper.Pi)是速度变化角度（基准为0.5pi，前面随速度变化不需要），1 * (1 - n*f * i)是大小逐级递减（也不需要）
						Main.EntitySpriteDraw(Ice, oldPos[i] - Main.screenPosition + new Vector2(0, 3) + new Vector2(0, (Projectile.frame * Ice.Height / 6 / 2)).RotatedBy(Projectile.rotation), new Rectangle(0, (int)(Projectile.frame * Ice.Height / 6f), Ice.Width, Ice.Height / 6), Color.White * (1 - 0.05f * i), Projectile.rotation, new Vector2(Ice.Width / 2, (Projectile.frame + 1) * (Ice.Height / 6) / 2), 1f, SpriteEffects.None, 0);
					}
				}
			}
			Main.EntitySpriteDraw(Ice, Projectile.Center - Main.screenPosition + new Vector2(0, 3) + new Vector2(0, (Projectile.frame * Ice.Height / 6 / 2)).RotatedBy(Projectile.rotation), new Rectangle(0, (int)(Projectile.frame * Ice.Height / 6f), Ice.Width, Ice.Height / 6), Color.White, Projectile.rotation, new Vector2(Ice.Width / 2, (Projectile.frame + 1) * (Ice.Height / 6) / 2), 1f, SpriteEffects.None, 0);

		}

		public override bool CanHitPlayer(Player Player) {
			return Projectile.frame == 5 && new Vector2(Player.Center.X - Projectile.Center.X, Player.Center.Y - Projectile.Center.Y).Length() <= 12f;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) {
			Projectile.Kill();
		}

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public override void OnKill(int timeLeft) {
			Vector2 position = Projectile.Center + new Vector2(0, -32f).RotatedBy(Projectile.rotation);
			SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNIceRingStg1") with { Volume = 1f, Pitch = 0f }, Projectile.Center);

			for (int i = 0; i < 5; i++) {
				Dust.NewDust(position, 16, 16, DustID.Granite, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1.25f);
				Dust.NewDust(position, 16, 16, DustID.PortalBolt, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1.25f);
			}
		}

		public override void AI() {//ai0为icenum（总数），1为numshooted（编号，0开始），2为技能种类
			if (formationShard) { RunFormationShard(); return; }
			timer++;
			if ((int)Projectile.ai[2] != Projectile.ai[2]) {//二阶段
				offsettime = (int)Projectile.ai[2] % 4 == 1 ? 4 : 2;
			}
			else {//一阶段
				offsettime = Projectile.ai[2] % 4 == 1 ? 8 : 4;
			}

			deltatime = (Projectile.ai[0] - Projectile.ai[1]) * offsettime;
			Projectile.rotation = (int)Projectile.ai[2] % 4 == 2 ? Projectile.ai[1] * 2 * float.Pi / Projectile.ai[0] + float.Pi : Projectile.ai[1] * 2 * float.Pi / Projectile.ai[0];
			if (Projectile.frame != 5) {
				if (Projectile.frameCounter >= 5) {
					Projectile.frameCounter = 0;
					Projectile.frame += 1;
				}
				else {
					if (Projectile.ai[0] == 0) {
						Projectile.frameCounter++;
					}
					else {

						if (timer >= deltatime) {
							Projectile.frameCounter++;
						}
					}
				}
			}
			else {
				Projectile.velocity = new Vector2(0, -Math.Min(((timer - deltatime) - 20f) / 5f, 16f)).RotatedBy(Projectile.rotation);
				if (timer % 1 == 0) {
					for (int i = oldPos.Length - 1; i > 0; i--) {
						oldPos[i] = oldPos[i - 1];
					}
					oldPos[0] = Projectile.Center;
				}

				Dust dust1;
				Vector2 position1 = Projectile.Center + new Vector2(8f * (float)Math.Sin((timer - deltatime) * Math.PI / 18f), -24f).RotatedBy(Projectile.rotation);
				dust1 = Terraria.Dust.NewDustPerfect(position1, 240, new Vector2(0, 0), 0, new Color(255, 255, 255), 1.5f);
				dust1.noGravity = true;

				Dust dust2;
				Vector2 position2 = Projectile.Center + new Vector2(-8f * (float)Math.Sin((timer - deltatime) * Math.PI / 18f), -24f).RotatedBy(Projectile.rotation);
				dust2 = Terraria.Dust.NewDustPerfect(position2, 263, new Vector2(0, 0), 0, new Color(255, 255, 255), 1.5f);
				dust2.noGravity = true;
			}
		}
	}
	//冰晶（引物）
	public partial class BlackIceIntro : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 15;
		}
		public override void SetDefaults() {
			Projectile.width = 46;
			Projectile.height = 36;
			Projectile.aiStyle = 0;
			Projectile.damage = 10;
			Projectile.timeLeft = 600;
			Projectile.penetrate = -1;
			Projectile.scale = 1f;
			Projectile.Opacity = 0f;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.netUpdate = true;
		}

		private float timer;
		private bool isProjShooted = false;
		private float deltatime;
		private float skilloffsetrotation;
		private Vector2 IceVelocity;

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public override void AI() {//ai0为0时是平A召唤，否则为技能召唤
			timer++;
			deltatime = (Projectile.ai[0] - Projectile.ai[1]) * 8;//越晚越短，ai0是总数，ai1是编号
			Projectile.Opacity = Math.Min(timer / 30f, 1f);
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];

			if (Projectile.frame < 12) {
				Projectile.rotation = Projectile.DirectionTo(Player.Center).ToRotation();
			}
			else if (Projectile.frame == 12) {
				skilloffsetrotation = Projectile.ai[0] == 0 ? 0 : 0.075f * float.Pi * MathF.Sin(2 * Projectile.ai[1] * float.Pi / Projectile.ai[0]);
				IceVelocity = new Vector2(0, -Math.Min(((timer - deltatime) - 20f) / 5f, 16f)).RotatedBy(Projectile.rotation + skilloffsetrotation);
				if (!isProjShooted) {
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, IceVelocity, ModContent.ProjectileType<BlackIce>(), Projectile.damage, 0.25f, -1, skilloffsetrotation);
					isProjShooted = true;
				}
			}
			//帧绘制
			if (Projectile.frameCounter >= 2) {
				Projectile.frameCounter = 0;
				Projectile.frame += 1;
				if (Projectile.frame == 14) {
					Projectile.Kill();
				}
			}
			else {
				if (Projectile.ai[0] == 0) {
					Projectile.frameCounter++;
				}
				else {

					if (timer >= deltatime) {
						Projectile.frameCounter++;
					}
				}
			}
		}
	}
	//冰晶（飞行）
	public partial class BlackIce : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 1;
		}
		public override void SetDefaults() {
			Projectile.width = 24;
			Projectile.height = 22;
			Projectile.aiStyle = 0;
			Projectile.damage = 10;
			Projectile.timeLeft = 65536;
			Projectile.penetrate = 1;
			Projectile.scale = 1f;
			Projectile.Opacity = 1f;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.netUpdate = true;
		}

		private float timer;
		private readonly Vector2[] oldPos = new Vector2[13]; //残影个数

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) {
			Projectile.Kill();
		}

		public override void PostDraw(Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D Ice = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/BlackIce").Value;
			for (int i = oldPos.Length - 1; i > 0; i--) {
				if (oldPos[i] != Vector2.Zero && i % 3 == 0) {//1%3是为了增大残影绘制间隔；Color.White * 1 * (1 - m*f * i)是残影淡化； (oldPos[i - 1] - oldPos[i]).ToRotation() + (float)(0.5 * MathHelper.Pi)是速度变化角度（基准为0.5pi，前面随速度变化不需要），1 * (1 - n*f * i)是大小逐级递减（也不需要）
					Main.EntitySpriteDraw(Ice, oldPos[i] - Main.screenPosition, null, Color.White * (1 - 0.05f * i), Projectile.rotation, Ice.Size() * 0.5f, 1f, SpriteEffects.None, 0);
				}
			}
		}

		public override void AI() {
			timer++;
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			if (timer <= 1f) {
				Projectile.rotation = Projectile.DirectionTo(Player.Center).ToRotation() + MathHelper.PiOver4 + Projectile.ai[0];
			}
			else {
				Projectile.velocity = new Vector2(0, -Math.Min(timer / 5f + 4f, 16f)).RotatedBy(Projectile.rotation + MathHelper.PiOver4);
				if (timer % 1 == 0) {
					for (int i = oldPos.Length - 1; i > 0; i--) {
						oldPos[i] = oldPos[i - 1];
					}
					oldPos[0] = Projectile.Center;
				}

				Dust dust1;
				Vector2 position1 = Projectile.Center + new Vector2(8f * (float)Math.Sin(timer * Math.PI / 18f), -12f).RotatedBy(Projectile.rotation + MathHelper.PiOver4);
				dust1 = Terraria.Dust.NewDustPerfect(position1, 240, new Vector2(0, 0), 0, new Color(255, 255, 255), 1f);
				dust1.noGravity = true;
				Dust dust2;
				Vector2 position2 = Projectile.Center + new Vector2(-8f * (float)Math.Sin(timer * Math.PI / 18f), -12f).RotatedBy(Projectile.rotation + MathHelper.PiOver4);
				dust2 = Terraria.Dust.NewDustPerfect(position2, 263, new Vector2(0, 0), 0, new Color(255, 255, 255), 1f);
				dust2.noGravity = true;
			}
		}

		public override void OnKill(int timeLeft) {
			Vector2 position = Projectile.Center + new Vector2(0, -12f).RotatedBy(Projectile.rotation);
			global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BlackIceExplode>(), Projectile.damage, 0.25f, -1, Projectile.ai[0]);
			for (int i = 0; i < 3; i++) {
				Dust.NewDust(position, 16, 16, DustID.Granite, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1f);
				Dust.NewDust(position, 16, 16, DustID.PortalBolt, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1f);
			}
		}
	}
	//冰晶爆炸
	public class BlackIceExplode : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 6;
		}
		public override void SetDefaults() {
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.aiStyle = 0;
			Projectile.damage = 20;
			Projectile.timeLeft = 60;
			Projectile.penetrate = -1;
			Projectile.scale = 1f;
			Projectile.Opacity = 1f;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.netUpdate = true;
		}

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public override void AI() {
			Projectile.frameCounter++;
			if (Projectile.frameCounter >= 6) {
				Projectile.frameCounter = 0;
				Projectile.frame += 1;
				if (Projectile.frame == 1) {
					if (Projectile.ai[0] == 0) {
						SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNAttack") with { Volume = 1f, Pitch = 0f }, Projectile.Center);
					}
					else {
						SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNIceRingStg1") with { Volume = 1f, Pitch = 0f }, Projectile.Center);
					}
				}
				if (Projectile.frame == 5) {
					Projectile.Kill();
				}
			}
		}
	}
	//技能分发弹幕，整合了8种冰环技能
	public partial class FNIceRingChoice : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 60;
		}

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 420;
			Projectile.alpha = 0;
			Projectile.damage = 10;
			Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 1f;
		}

		private float timer;//计时器
		private int icenum;//冰凌个数
		private int icenumshooted;
		private float speedY;
		private NPC HostNPC = null;

		//碰上就冻结
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public static int CHildProjType1() {
			return ModContent.ProjectileType<FNIceRingRound>();
		}

		public static int HostNPCType() {
			return ModContent.NPCType<FrostNova>();
		}

		public override void OnSpawn(IEntitySource source) {
			bool hasChild = false;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile ChildProj = Main.projectile[i];
				if (ChildProj.active && ChildProj.ai[1] == Projectile.whoAmI && ChildProj.type == CHildProjType1()) {
					hasChild = true;
				}
			}
			if (!hasChild) {
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(source, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FNIceRingRound>(), Projectile.damage, 0f, -1, -1, Projectile.whoAmI);
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(source, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FNIceRingRound>(), Projectile.damage, 0f, -1, 1, Projectile.whoAmI);
			}
			//寻找主体
			for (int i = 0; i < Main.maxNPCs; i++) {
				HostNPC = Main.npc[i];
				if (HostNPC.active && HostNPC.type == HostNPCType()) {
					break;
				}
			}
			//修正冰晶数量
			if (Projectile.ai[1] % 4 == 0) {//交火冰环
				icenum = Main.rand.Next(8, 13);
			}
			else if (Projectile.ai[1] % 4 == 2) {//坠冰冰环
				icenum = Projectile.ai[0] == 1 ? 45 : 90;
			}
			else {//螺旋冰环和辐向冰环
				icenum = Projectile.ai[0] == 1 ? Main.rand.Next(16, 25) : Main.rand.Next(32, 49);
			}
		}

		public override void AI() {
			if (formationConfigured) { RunFormation(); return; }
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			var newSource = Projectile.GetSource_FromThis();
			timer++;

			if (Projectile.ai[1] % 4 == 0) {//交火冰环（α和α+）
				speedY = timer >= 240f ? 0 : (Projectile.ai[2] == 0 ? -2 : -2 * MathF.Sqrt(2)) * MathF.Sin(float.Pi * timer / 240f);//960 / π像素
				Projectile.velocity = new Vector2(0, speedY).RotatedBy(Projectile.ai[2]);
				if (timer >= 300f && (int)(timer - 300) % 8 == 0 && icenumshooted < icenum) {
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center + new Vector2(0, -72f).RotatedBy(icenumshooted * 2 * float.Pi / icenum) + new Vector2(1f, 14f).RotatedBy(0), Vector2.Zero, ModContent.ProjectileType<BlackIceIntro>(), Projectile.damage, 0f, -1, icenum, icenumshooted);
					SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
					icenumshooted++;
				}
			}
			else if (Projectile.ai[1] % 4 == 2) {//坠冰冰环（γ和γ+）
				float dis = timer >= 120f ? (Projectile.Center - HostNPC.Center).Length() : 0;

				if (timer <= 120f) {
					speedY = (Projectile.ai[0] == 1 ? -5.25f : -7.25f) * MathF.Sin(float.Pi * timer / 120f);//1260(2520) / π像素
					Projectile.velocity = new Vector2(0, speedY);
				}
				else if (timer <= 300f) {
					speedY = 2f / 9f * (timer - 120f) / 60f * (timer - 120f) / 60f * float.Pi;//绕一整圈
					Projectile.Center = HostNPC.Center + new Vector2(0, -dis).RotatedBy(speedY);
				}
				else if (timer <= 390f) {
					speedY = (timer - 300) * float.Pi / 45f;
					Projectile.Center = HostNPC.Center + new Vector2(0, -dis).RotatedBy(speedY);//绕一整圈
				}
				else {
					Projectile.Center = HostNPC.Center + new Vector2(0, -dis).RotatedBy(speedY);
				}

				if (timer >= 300f && (int)(timer - 300) % (Projectile.ai[0] == 1 ? 2 : 1) == 0 && timer < 390f) {
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center + new Vector2(0, 32f).RotatedBy((icenumshooted) * 2 * float.Pi / icenum), Vector2.Zero, ModContent.ProjectileType<BlackIceProjectile>(), Projectile.damage, 0f, -1, icenum, icenumshooted - 1, Projectile.ai[1]);
					if ((int)(timer - 300) % 2 == 0) {
						SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
					}
					icenumshooted++;
				}
			}
			else {//周身冰环（β和δ）,ai1（技能次数）会传给子弹幕的ai2，除以4余1的时候是不同时间发射的冰环（β），除以4余3是同时间发射的冰环（δ）
				Projectile.Center = HostNPC.Center;
				if (timer >= 300f && (int)(timer - 300) % (Projectile.ai[0] == 1 ? 4 : 2) == 0 && icenumshooted < icenum) {
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center + new Vector2(0, -72f).RotatedBy(icenumshooted * 2 * float.Pi / icenum) + new Vector2(1f, 0f).RotatedBy(0), Vector2.Zero, ModContent.ProjectileType<BlackIceProjectile>(), Projectile.damage, 0f, -1, icenum, icenumshooted, Projectile.ai[1] + 0.1f * (Projectile.ai[0] - 1));
					SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
					icenumshooted++;
				}
			}

			if (Main.rand.NextFloat() < Math.Min(timer / 300, 0.25f)) {
				Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
			}
		}
	}
	//环绕弹幕
	public partial class FNIceRingRound : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 60;
		}

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 900;
			Projectile.alpha = 0;
			Projectile.damage = 10;
			Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 1f;
		}

		private float timer;//计时器
		private float dis;
		private float drawopacity;
		private int r;
		private int g;
		private Projectile HostProj = null;

		//碰上就冻结
		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public static int HostProjType1() {
			return ModContent.ProjectileType<FNIceRingChoice>();
		}

		public override void OnSpawn(IEntitySource source) {
			for (int i = 0; i < Main.maxProjectiles; i++) {
				HostProj = Main.projectile[i];
				if (HostProj.active && Projectile.ai[1] == HostProj.whoAmI && HostProj.type == HostProjType1()) {
					break;
				}
			}
		}

		public override void AI() {
            int hostSlot = (int)Projectile.ai[1];
            if (!Main.projectile.IndexInRange(hostSlot) || !Main.projectile[hostSlot].active || Main.projectile[hostSlot].ModProjectile is not FNIceRingChoice) {
                Projectile.hostile = false;
                if (Main.netMode != NetmodeID.MultiplayerClient || timer++ > 90) Projectile.Kill();
                return;
            }
            HostProj = Main.projectile[hostSlot];
            Projectile.hostile = false;
			timer++;
			Projectile.velocity = Vector2.Zero;
			dis = Math.Min(timer / 4, 36f);

			Projectile.timeLeft = HostProj.timeLeft;
			Projectile.Center = HostProj.Center + new Vector2(Projectile.ai[0] * dis, 0).RotatedBy(timer * float.Pi / 45);

			if (timer >= 240f) {
				Projectile.ai[2]--;
			}
			else if (timer <= 30f) {
				Projectile.ai[2] += 2f;
			}
			else {
				Projectile.ai[2] = 60;
			}
			drawopacity = Math.Min(Projectile.ai[2] / 60f, 1f);

			if (drawopacity == 0) {
				Projectile.Kill();
			}

			r = 235 + 20 * (int)MathF.Sin(timer * MathHelper.Pi / 180);
			g = 245 + 10 * (int)MathF.Sin(timer * MathHelper.Pi / 180);

			if (Main.rand.NextFloat() < Math.Min(timer / 960, 0.25f)) {
				Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
			}
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/WindTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color(r, g, 255) * drawopacity, new Color(0, 0, 0), 25f, true);
			return true;
		}
	}
	//无敌特效
	public partial class FNInvincibleEffect : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 60;
		}

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 900;
			Projectile.alpha = 0;
			Projectile.damage = 0;
			Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		private float timer;
		private float drawopacity;
		private float dotX;
		private float dotY;
		private int r;
		private int g;
		private int b;

		private NPC HostNPC = null;

		public static int HostNPCType() {
			return ModContent.NPCType<FrostNova>();
		}

		//寻找主体
		public override void OnSpawn(IEntitySource source) {
			Projectile.timeLeft = (int)Projectile.ai[0];
			for (int i = 0; i < Main.maxNPCs; i++) {
				HostNPC = Main.npc[i];
				if (HostNPC.active && HostNPC.type == HostNPCType()) {
					break;
				}
			}
		}

		public override void AI() {
			timer++;
			Projectile.velocity = Vector2.Zero;
			dotX = 36 * MathF.Sin((timer / 30 + 0.4f + 0.6f * Projectile.ai[1]) * float.Pi);
			dotY = 36 * Projectile.ai[1] * 0.5f * MathF.Cos((timer / 30 + 0.6f * Projectile.ai[1]) * float.Pi);
			Projectile.Center = HostNPC.Center + new Vector2(dotX, dotY + 8);

			if (timer <= 60f) {
				drawopacity += 1 / 60f;
			}
			else if (Projectile.timeLeft <= 60f) {
				drawopacity -= 1 / 60f;
			}
			else {
				drawopacity = 1;
			}

			r = 235 + 20 * (int)MathF.Sin(timer * MathHelper.Pi / 180);
			g = 245 + 10 * (int)MathF.Sin(timer * MathHelper.Pi / 180);

			if (!HostNPC.active) {
				Projectile.Kill();
			}
			if (Projectile.timeLeft > 60) {
				Projectile.timeLeft = HostNPC.dontTakeDamage ? 1800 : 60;
			}
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/LineTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color(r, g, 0) * drawopacity, new Color(0, 0, 0), 15f, true);
			return true;
		}
	}
	//封印地板的冰柱
	public partial class FNBanFloor : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 1;
		}
		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 70;
			Projectile.aiStyle = 0;
			Projectile.damage = 10;
			Projectile.timeLeft = 65536;
			Projectile.penetrate = 1;
			Projectile.scale = 0f;
			Projectile.Opacity = 0f;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.netUpdate = true;
		}

		private float timer;
		private readonly Vector2[] oldPos = new Vector2[13]; //残影个数
		private float GateScaleX = 0f;
		private float GateScaleY = 2f;
		private float GateOpacity = 0f;
		private float MaskOpacity = 0f;
		private float ProjOpacity = 0f;
		private float Projscale = 0f;
		private Vector2 SpawnPos;

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public override void OnSpawn(IEntitySource source) {
			SpawnPos = Projectile.Center;
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D Gate = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/FNBanFloorGate").Value;
			Vector2 GateScale = new Vector2(GateScaleX, GateScaleY);
			Main.EntitySpriteDraw(Gate, SpawnPos - Main.screenPosition, new Rectangle(0, 0, Gate.Width, Gate.Height), Color.White * GateOpacity, 0, new Vector2(Gate.Width / 2, Gate.Height / 2), GateScale, SpriteEffects.None, 0);
			return true;
		}

		public override void PostDraw(Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D Ice = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/FNBanFloor").Value;
			Texture2D Mask = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/FNBanFloorMask").Value;
			Main.EntitySpriteDraw(Ice, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, Ice.Width, Ice.Height), Color.White * ProjOpacity, Projectile.rotation, new Vector2(Ice.Width / 2, Ice.Height / 2), Projscale, SpriteEffects.None, 0);

			if (timer > 60 && timer <= 90) {
				Main.EntitySpriteDraw(Mask, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, Mask.Width, Mask.Height), Color.White * MaskOpacity, Projectile.rotation, new Vector2(Mask.Width / 2, Mask.Height / 2), 1f, SpriteEffects.None, 0);
			}
			else if (timer > 90) {
				for (int i = oldPos.Length - 1; i > 0; i--) {
					if (oldPos[i] != Vector2.Zero && i % 3 == 0) {//1%3是为了增大残影绘制间隔；Color.White * 1 * (1 - m*f * i)是残影淡化； (oldPos[i - 1] - oldPos[i]).ToRotation() + (float)(0.5 * MathHelper.Pi)是速度变化角度（基准为0.5pi，前面随速度变化不需要），1 * (1 - n*f * i)是大小逐级递减（也不需要）
						Main.EntitySpriteDraw(Ice, oldPos[i] - Main.screenPosition, null, Color.White * (1 - 0.05f * i), Projectile.rotation, Ice.Size() * 0.5f, 1f, SpriteEffects.None, 0);
					}
				}
			}
		}

		public override void AI() {
			timer++;
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			if (timer <= 30f) {
				GateScaleX = timer / 60f;
				GateScaleY = 2f + timer / 15f;
				GateOpacity = timer / 30f;
			}
			else if (timer <= 60f) {
				ProjOpacity = (timer - 30f) / 30f;
				Projscale = (timer - 30f) / 30f;
			}
			else if (timer <= 90f) {
				GateScaleX = 1f - (timer - 60f) / 30f;
				GateScaleY = 4f - (timer - 60f) / 15f;
				GateOpacity = 1f - (timer - 60f) / 30f;
				if (timer > 60f && timer <= 75f) {
					MaskOpacity = (timer - 75f) / 15f;
				}
				else if (timer > 75f && timer <= 90f) {
					MaskOpacity = (90f - timer) / 15f;
				}
			}
			else {
				Projectile.velocity = new Vector2(0, Math.Min((timer - 60f) / 5f + 4f, 16f));
				if (timer % 1 == 0) {
					for (int i = oldPos.Length - 1; i > 0; i--) {
						oldPos[i] = oldPos[i - 1];
					}
					oldPos[0] = Projectile.Center;
				}

			}
			if (timer == 5) {
				SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNSummonLockTileIce") with { Volume = 1f, Pitch = 0f }, Projectile.Center);
			}
			if (timer <= 60f) {
				Projectile.velocity.Y = -timer / 60f;
			}
		}

		public override void OnKill(int timeLeft) {
			Vector2 position = Projectile.Center + new Vector2(0, -12f).RotatedBy(Projectile.rotation);
			SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNLockTileIce") with { Volume = 1f, Pitch = 0f }, Projectile.Center);
			for (int i = 0; i < 3; i++) {
				Dust.NewDust(position, 16, 16, DustID.Granite, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1f);
				Dust.NewDust(position, 16, 16, DustID.PortalBolt, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1f);
			}
			global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ProjectileType<BlackIceFloor>(), 20, 0f, -1, Main.rand.Next(0, 12));
		}
	}
	//被封印的地板
	public class BlackIceFloor : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 12;
		}
		public override void SetDefaults() {
			Projectile.width = 60;
			Projectile.height = 32;
			Projectile.aiStyle = 0;
			Projectile.damage = 20;
			Projectile.timeLeft = 60000;
			Projectile.penetrate = -1;
			Projectile.scale = 1f;
			Projectile.Opacity = 1f;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.netUpdate = true;
		}

		public static int HostNPCType() {
			return ModContent.NPCType<FrostNova>();
		}

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			FrostNovaChill.Apply(target, 120, 30);
		}

		public override void AI() {
			Projectile.frame = (int)Projectile.ai[0];

			if (Projectile.timeLeft > 30) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC SeekForNPCs = Main.npc[i];
					if (SeekForNPCs.active && SeekForNPCs.type == HostNPCType()) {
						if (SeekForNPCs.life == 1) {
							Projectile.timeLeft = 30;
						}
						else {
							Projectile.timeLeft = 60;
						}
					}
				}
			}
			else {

			}
		}

		public override void OnKill(int timeLeft) {
			SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/IceCrystal") with { Volume = 1f, Pitch = 0f }, Projectile.Center);
			for (int i = 0; i < 5; i++) {
				Dust.NewDust(Projectile.Center, 16, 16, DustID.Granite, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1f);
				Dust.NewDust(Projectile.Center, 16, 16, DustID.PortalBolt, -0.5f * Projectile.velocity.X, -0.5f * Projectile.velocity.Y, 0, new Color(255, 255, 255), 1f);
			}
		}
	}
	//冷气粒子效果
	public partial class FNIceDust : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 60;
		}

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 190;
			Projectile.alpha = 0;
			Projectile.damage = 0;
			Projectile.light = 0f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		private float timer;
		private float drawopacity;
		private float dotX;
		private float dotY;
		private int r;
		private int g;
		private int b;

		private NPC HostNPC = null;

		public static int HostNPCType() {
			return ModContent.NPCType<FrostNova>();
		}

		//寻找主体
		public override void OnSpawn(IEntitySource source) {
			for (int i = 0; i < Main.maxNPCs; i++) {
				HostNPC = Main.npc[i];
				if (HostNPC.active && HostNPC.type == HostNPCType()) {
					break;
				}
			}
		}

		public override void AI() {
			timer++;
			float k = Projectile.ai[1] * float.Pi / 18;
			float radius = 300;
			float dX = -radius * MathF.Sin(Projectile.ai[0] * float.Pi / 3 + k) + radius * MathF.Sin(((float.Pi * timer) / 90) + Projectile.ai[0] * float.Pi / 3 + k);
			float dY = radius * MathF.Cos(Projectile.ai[0] * float.Pi / 3 + k) - radius * MathF.Cos(((float.Pi * timer) / 90) + Projectile.ai[0] * float.Pi / 3 + k);
			Projectile.Center = HostNPC.Center + new Vector2(dX, dY);

			if (timer <= 60f) {
				drawopacity += 1 / 360f;
			}
			else if (Projectile.timeLeft <= 60f) {
				drawopacity -= 1 / 360f;
			}
			else {
				drawopacity = 1 / 6f;
			}

			r = 185 + 20 * (int)MathF.Sin(timer * MathHelper.Pi / 180);
			g = 245 + 10 * (int)MathF.Sin(timer * MathHelper.Pi / 180);

			Dust.NewDustDirect(Projectile.Center, 0, 0, DustType<Dusts.Bosses.FrostNovaDeathDust>(), 0f, 0f, 120, new Color(255, 255, 255), 0.5f);
		}

		public override bool PreDraw(ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */ {
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/WindTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color(r, g, 255) * drawopacity, new Color(0, 0, 0), 100f, true);
			return true;
		}
	}
}