using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.BossBars;
using ArknightsMod.Content.Items.Material;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer
{
	public partial class AACTIntro : global::ArknightsMod.Common.Networking.LegacyEnemyNetwork
	{
		public override string Texture => ArknightsMod.noTexture;
		public override void SetStaticDefaults() {
			Main.npcFrameCount[NPC.type] = 1;//贴图帧数
		}

		public override void SetDefaults() {
			NPC.lifeMax = 1;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.knockBackResist = 0f;//击退抗性，0f为最高，1f为最低
			NPC.width = 1;
			NPC.height = 1;
			NPC.noGravity = true;//无引力
			NPC.noTileCollide = true;//不与物块相撞
			NPC.lavaImmune = true;//免疫岩浆
			NPC.aiStyle = -1;
			NPC.HitSound = SoundID.NPCHit4;//金属声
			NPC.DeathSound = SoundID.NPCDeath14;//爆炸声
			NPCID.Sets.ImmuneToAllBuffs[Type] = true;//免疫所有debuff
			NPC.friendly = true;
			NPC.dontTakeDamageFromHostiles = true;
			NPC.dontTakeDamage = true;
		}

		private float timer = 0;
		public override void AI() {
			Player Player = Main.player[NPC.target];
			NPC.Center = Player.Center;
			timer++;
			if (timer == 330) {
				Main.NewText(Language.GetTextValue("钀ㄧ背鍥犻潪鍐板師鐩樿笧鐨勯偑榄�"), 138, 0, 18);
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewNPC(Terraria.Entity.GetSource_TownSpawn(), (int)Player.Center.X, (int)Player.Center.Y - 1200, NPCType<AACT>());
				NPC.life = 0;
			}
		}
	}

	public class AACTDeathdust : ModProjectile//死亡粒子效果触发器
	{
		public override string Texture => ArknightsMod.noTexture;
		public override void SetStaticDefaults() {
		}
		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 60;
			Projectile.alpha = 0;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
		}

		public override void AI() {
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			Player.GetModPlayer<ShakeEffectPlayer>().screenShakeOnlyOnY = true;//只在纵向震动
			Player.GetModPlayer<ShakeEffectPlayer>().screenShakeTime = 30;
			Vector2 dustPos = Projectile.Center + new Vector2(Main.rand.NextFloat(10), 0).RotatedByRandom(Math.PI);
			for (int i = 0; i < 5; i++) {
				Dust deathdust1 = Dust.NewDustPerfect(dustPos, DustID.PurificationPowder, Velocity: Vector2.Zero, Scale: 2f);
				deathdust1.velocity = 5 * dustPos - 5 * Projectile.Center;
				Dust deathdust2 = Dust.NewDustPerfect(dustPos, DustID.VilePowder, Velocity: Vector2.Zero, Scale: 2f);
				deathdust2.velocity = 5 * dustPos - 5 * Projectile.Center;
				Dust deathdust3 = Dust.NewDustPerfect(dustPos, DustID.Firework_Blue, Velocity: Vector2.Zero, Scale: 2.5f);
				deathdust3.velocity = 4 * dustPos - 4 * Projectile.Center;
				Dust deathdust4 = Dust.NewDustPerfect(dustPos, DustID.Firework_Pink, Velocity: Vector2.Zero, Scale: 2.5f);
				deathdust4.velocity = 4 * dustPos - 4 * Projectile.Center;
			}
		}
	}

	public partial class CollapsedHitboxblueCore : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 8;
		}
		public override void SetDefaults() {
			Projectile.width = 110;
			Projectile.height = 110;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 120;
			Projectile.alpha = 50;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
		}

		private float click = 0;
		private float prefire;
		private int randt = 1;
		private int frametimer;
		private int skilltimer = 0;

		public override void AI()//ai
		{
			var newSource = Projectile.GetSource_FromThis();
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			click++;
			skilltimer++;
			frametimer++;
			if (frametimer > 3) {
				randt = Main.rand.Next(2, 31);
				frametimer = 0;
			}
			Projectile.frame += (int)Main.time % randt == 0 ? Main.rand.Next(1, 9) : 0;
			Projectile.frame %= 8;

			if (Main.masterMode) {
				prefire = 48;
			}
			else if (Main.expertMode) {
				prefire = 42;
			}
			else {
				prefire = 36;
			}

			if (Projectile.scale > 1f) {
				Projectile.scale = 1f;
			}
			if (Projectile.scale < 0f) {
				Projectile.scale = 0f;
			}
			if (Projectile.alpha < 0) {
				Projectile.alpha = 0;
			}
			if (Projectile.alpha > 255) {
				Projectile.alpha = 255;
			}

			Projectile.Center = Player.Center + prefire * Player.velocity + new Vector2(Main.rand.NextFloat(-64, 64), Main.rand.NextFloat(-64, 64));
			Projectile.scale = 3.3f / (click / 24 + 0.5f) / (click / 24 - 5.5f) + 1.4f;
			Projectile.alpha = (int)(0.1 * click * click - 12 * click + 255);

			if (skilltimer == 120) {
				if (Player.velocity.X * Player.velocity.X + Player.velocity.Y * Player.velocity.Y >= 100) {
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center.X + 0.25f * prefire * Player.velocity.X, Projectile.Center.Y + 0.25f * prefire * Player.velocity.Y, 0, 0, ProjectileType<CollapsedExplodeAim>(), 0, 0, 0, 0);
				}
				else {
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center.X, Projectile.Center.Y, 0, 0, ProjectileType<CollapsedExplodeAim>(), 0, 0, 0, 0);
				}
				skilltimer = 0;
			}

		}
	}

	public partial class CollapsedExplodeAim : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 8;
		}
		public override void SetDefaults() {
			Projectile.width = 110;
			Projectile.height = 110;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 85;
			Projectile.alpha = 10;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		private float randomx;
		private bool missled = false;
		private float timer;
		private int randt = 1;
		private int frametimer;
		private Vector2 randvelocity;
		private float randspeed;
		private float randrotation;
		private bool summonedframes = false;
		private float[,] VelocityABCDxy = new float[4, 2] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };//存储四个顶点的发射速度
		private float[] MoveLengthABCD = new float[4] { 0, 0, 0, 0 };//存储四个弹幕的位移
		private float[,] RectangleABCDxy = new float[4, 2] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };//存储四个顶点的位置坐标
		private static Vector2 fixedBCDxy = new Vector2(192, 192);
		private float r;
		private float g;
		private float b;
		private float a;

		private static float CalcPos(float velocity, float time) {
			return velocity * time * (1 - time / 120);
		}

		public override void AI() {
			var newSource = Projectile.GetSource_FromThis();
			Player player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			frametimer++;
			if (frametimer > 3) {
				randt = Main.rand.Next(2, 31);
				frametimer = 0;
			}
			Projectile.frame += (int)Main.time % randt == 0 ? Main.rand.Next(1, 9) : 0;
			Projectile.frame %= 8;

			timer++;

			if (timer >= 0 && timer <= 10) {
				Projectile.scale = (float)Math.Sin(Math.PI * timer / 20f);
			}
			else if (timer > 10 && timer <= 60) {
				Projectile.scale = 1f;
			}
			else if (timer > 60 && timer <= 75) {
				Projectile.scale = (float)Math.Cos(Math.PI * timer / 30f);
			}

			if (timer >= 0 && timer <= 10) {
				Projectile.alpha = (int)(120 * Math.Cos(Math.PI * timer / 10f) + 120);
			}
			else if (timer > 10 && timer <= 30) {
				Projectile.alpha = 0;
			}
			else if (timer > 30 && timer <= 60) {
				Projectile.alpha = (int)(-120 * Math.Cos(Math.PI * timer / 5f) + 120);
			}
			else if (timer > 60 && timer <= 75) {
				Projectile.alpha = (int)(-120 * Math.Cos(Math.PI * timer / 15f) + 120);
			}

			if (timer == 10) {// >= 10
							  //if(summonedframes == false) {
				for (int i = 0; i < 4; i++) {
					if (Main.masterMode) {
						randspeed = Main.rand.NextFloat(9.6f, 10.8f);
					}
					else if (Main.expertMode) {
						randspeed = Main.rand.NextFloat(7.5f, 10f);
					}
					else {
						randspeed = Main.rand.NextFloat(6f, 8f);
					}
					randrotation = i * MathHelper.PiOver2 + Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
					randvelocity = new Vector2(randspeed, 0).RotatedBy(randrotation);
					VelocityABCDxy[i, 0] = randspeed;//存储每个顶点的发射速度向量模长
					VelocityABCDxy[i, 1] = randrotation;//存储每个顶点的发射速度角度
					global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center, randvelocity, ProjectileType<CollapsedFrame>(), 0, 0f, -1, i, randvelocity.X, randvelocity.Y);
				}
				//summonedframes = true;
				//}
			}

			if (timer > 10) {
                // ScreenShaderData offsets its target by the actual offscreen border.
                // All four vertices must use the same transform, rather than the old fixed 192px.
                Vector2 frameOffset = Projectile.GetGlobalProjectile<HarbingerOwnedProjectile>().Owned
                    ? new Vector2(Main.offScreenRange) : fixedBCDxy;
				for (int i = 0; i < 4; i++) {
					MoveLengthABCD[i] = CalcPos(VelocityABCDxy[i, 0], timer - 10);//每个弹幕的运动长度（不是顶点）
					RectangleABCDxy[i, 0] = Projectile.Center.X + new Vector2(MoveLengthABCD[i], 0).RotatedBy(VelocityABCDxy[i, 1]).X + new Vector2(-18, 0).RotatedBy(VelocityABCDxy[i, 1]).X;//每个顶点的X坐标
					RectangleABCDxy[i, 1] = Projectile.Center.Y + new Vector2(MoveLengthABCD[i], 0).RotatedBy(VelocityABCDxy[i, 1]).Y + new Vector2(-18, 0).RotatedBy(VelocityABCDxy[i, 1]).Y;//每个顶点的Y坐标
				}
				//颜色修改
				if (timer <= 30) {
					r = 0;
					g = (30 - timer) / 20;
					b = 1;
					a = (timer - 10) / 40;
				}
				else if (timer <= 50) {
					r = 0.75f * (timer - 30) / 20;
					g = 0;
					b = 1;
					a = 0.2f * (float)Math.Cos(Math.PI * timer / 5f) + 0.3f;
				}
				else if (timer <= 70) {
					r = 0.75f + 0.25f * (timer - 50) / 20;
					g = (timer - 50) / 20;
					b = 1;
					a = 0.5f;
				}
				else {
					a = 0.5f * (85 - timer) / 15;
				}
				//喜闻乐见的傻der时刻
				if (!Main.dedServ && Main.netMode != NetmodeID.Server && !Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart1"].IsActive()) {
					ArknightsMod.CollapsedExplosionPart1.Value.Parameters["uImageSize1"].SetValue(new Vector2(RectangleABCDxy[1, 0], RectangleABCDxy[1, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart1.Value.Parameters["uImageSize2"].SetValue(new Vector2(RectangleABCDxy[2, 0], RectangleABCDxy[2, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart1.Value.Parameters["uImageSize3"].SetValue(new Vector2(RectangleABCDxy[3, 0], RectangleABCDxy[3, 1]) - frameOffset);
					Terraria.Graphics.Effects.Filters.Scene.Activate("CollapsedExplosionPart1", Projectile.Center).GetShader().UseColor(r, g, b).UseTargetPosition(new Vector2(RectangleABCDxy[0, 0], RectangleABCDxy[0, 1])).UseIntensity(36 * Projectile.scale).UseOpacity(a);
				}
				if (!Main.dedServ && Main.netMode != NetmodeID.Server && Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart1"].IsActive()) {
					ArknightsMod.CollapsedExplosionPart1.Value.Parameters["uImageSize1"].SetValue(new Vector2(RectangleABCDxy[1, 0], RectangleABCDxy[1, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart1.Value.Parameters["uImageSize2"].SetValue(new Vector2(RectangleABCDxy[2, 0], RectangleABCDxy[2, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart1.Value.Parameters["uImageSize3"].SetValue(new Vector2(RectangleABCDxy[3, 0], RectangleABCDxy[3, 1]) - frameOffset);
					Terraria.Graphics.Effects.Filters.Scene.Activate("CollapsedExplosionPart1", Projectile.Center).GetShader().UseColor(r, g, b).UseTargetPosition(new Vector2(RectangleABCDxy[0, 0], RectangleABCDxy[0, 1])).UseIntensity(36 * Projectile.scale).UseOpacity(a);
				}
				if (!Main.dedServ && Main.netMode != NetmodeID.Server && !Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart2"].IsActive()) {
					ArknightsMod.CollapsedExplosionPart2.Value.Parameters["uImageSize1"].SetValue(new Vector2(RectangleABCDxy[1, 0], RectangleABCDxy[1, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart2.Value.Parameters["uImageSize2"].SetValue(new Vector2(RectangleABCDxy[2, 0], RectangleABCDxy[2, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart2.Value.Parameters["uImageSize3"].SetValue(new Vector2(RectangleABCDxy[3, 0], RectangleABCDxy[3, 1]) - frameOffset);
					Terraria.Graphics.Effects.Filters.Scene.Activate("CollapsedExplosionPart2", Projectile.Center).GetShader().UseColor(r, g, b).UseTargetPosition(new Vector2(RectangleABCDxy[0, 0], RectangleABCDxy[0, 1])).UseIntensity(36 * Projectile.scale).UseOpacity(a);
				}
				if (!Main.dedServ && Main.netMode != NetmodeID.Server && Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart2"].IsActive()) {
					ArknightsMod.CollapsedExplosionPart2.Value.Parameters["uImageSize1"].SetValue(new Vector2(RectangleABCDxy[1, 0], RectangleABCDxy[1, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart2.Value.Parameters["uImageSize2"].SetValue(new Vector2(RectangleABCDxy[2, 0], RectangleABCDxy[2, 1]) - frameOffset);
					ArknightsMod.CollapsedExplosionPart2.Value.Parameters["uImageSize3"].SetValue(new Vector2(RectangleABCDxy[3, 0], RectangleABCDxy[3, 1]) - frameOffset);
					Terraria.Graphics.Effects.Filters.Scene.Activate("CollapsedExplosionPart2", Projectile.Center).GetShader().UseColor(r, g, b).UseTargetPosition(new Vector2(RectangleABCDxy[0, 0], RectangleABCDxy[0, 1])).UseIntensity(36 * Projectile.scale).UseOpacity(a);
				}
			}

			if (timer == 60) {
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, Projectile.Center.X, Projectile.Center.Y, 0, 0, ProjectileType<ExplodeWave>(), 10, 0f, 0, 0);
			}

			if (timer == 70) {
				var owner = Projectile.GetGlobalProjectile<HarbingerOwnedProjectile>();
				if (owner.Owned) {
					owner.Collapse(Projectile, RectangleABCDxy);
					if (!Main.dedServ) SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sound/ImperialArtilleyCoreTargeteer/IACTStageChangeTremor") with { Volume = 1f }, Projectile.Center);
					return;
				}
				SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sound/ImperialArtilleyCoreTargeteer/IACTStageChangeTremor") with { Volume = 2f, Pitch = 0f }, player.Center);
				//player.Hurt(PlayerDeathReason.ByCustomReason(player.name + "坍缩了。"), Main.rand.Next(60, 120),0);
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC SeekForNPCs = Main.npc[i];
					if (SeekForNPCs.active && SeekForNPCs.type == NPCType<AACT>()) {
						if (player.Center.X < RectangleABCDxy[0, 0] && player.Center.X > RectangleABCDxy[2, 0] && player.Center.Y < RectangleABCDxy[1, 1] && player.Center.Y > RectangleABCDxy[3, 1]) {
							player.Heal((int)-SeekForNPCs.ai[2]);
							SeekForNPCs.ai[2] = Math.Max(SeekForNPCs.ai[2] * Main.rand.NextFloat(2f, 4f), 2f);
							if (player.statLife <= 0) {
								player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + Language.GetTextValue("Mods.ArknightsMod.StatusMessage.AACT.Collapsed"))), 99999, 0);
							}
						}
						else {
							SeekForNPCs.ai[2] = Math.Max(SeekForNPCs.ai[2] * Main.rand.NextFloat(0.5f, 1f), 2f);
						}
					}
				}
			}
		}

		public override void OnKill(int timeLeft) {
			if (!Main.dedServ && Main.netMode != NetmodeID.Server && Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart1"].IsActive()) {
				Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart1"].Deactivate();
			}
			if (!Main.dedServ && Main.netMode != NetmodeID.Server && Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart2"].IsActive()) {
				Terraria.Graphics.Effects.Filters.Scene["CollapsedExplosionPart2"].Deactivate();
			}
		}
	}

	public partial class CollapsedFrame : ModProjectile
	{
		public override void SetStaticDefaults() {
			Main.projFrames[Projectile.type] = 8;
		}
		public override void SetDefaults() {
			Projectile.width = 64;
			Projectile.height = 72;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 75;
			Projectile.alpha = 255;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
		}

		private int timer;
		private int randt = 1;
		private float timer2;
		//private float randstop;

		public override void PostDraw(Color lightColor) {
			Texture2D Colline = Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/RoaringFlare/ImperialArtilleyCoreTargeteer/CollapsedLine", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			Main.EntitySpriteDraw(Colline, Projectile.Center - Main.screenPosition + new Vector2(0, 32 * Projectile.scale).RotatedBy(Projectile.rotation), new Rectangle(0, 0, Colline.Width, Colline.Height), Color.White, (Projectile.ai[0] - 1) * MathHelper.PiOver2, new Vector2(Colline.Width / 2, Colline.Height / 2), Projectile.scale, SpriteEffects.None, 0);
		}

		public override void AI() {
			//for (int i = 0; i < 1; i++) {
			//	if (Main.masterMode) {
			//		randstop = Main.rand.NextFloat(0.0375f, 0.05f);
			//	}
			//	else if (Main.expertMode) {
			//		randstop = Main.rand.NextFloat(0.05f, 0.075f);
			//	}
			//	else {
			//		randstop = Main.rand.NextFloat(0.0625f, 0.0875f);
			//	}
			//}
			timer++;
			timer2++;
			if (timer > 3) {
				randt = Main.rand.Next(2, 31);
				timer = 0;
			}

			if (timer2 <= 15) {
				Projectile.alpha = (int)(255 - timer2 * 17);
				Projectile.scale = timer2 / 15;
			}
			else if (timer2 <= 60) {
				Projectile.alpha = 0;
				Projectile.scale = 1f;
			}
			else {
				Projectile.alpha = (int)(timer2 - 60) * 17;
				Projectile.scale = 5 - timer2 / 15;
			}

			Projectile.frame += (int)Main.time % randt == 0 ? Main.rand.Next(1, 9) : 0;
			Projectile.frame %= 8;
			if (Projectile.timeLeft >= 15) {
				Projectile.velocity = new Vector2(Projectile.ai[1], Projectile.ai[2]) * (Projectile.timeLeft - 14) / 60;//一秒内减速到0
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			}
			else {
				Projectile.velocity = new Vector2(Projectile.ai[1], Projectile.ai[2]) * 0.01f;
			}
			//Projectile.velocity = Vector2.Lerp(Projectile.velocity,Vector2.Zero,randstop);
		}
	}

	public partial class AACTStage3TrueHealth : global::ArknightsMod.Common.Networking.LegacyEnemyNetwork
	{
		public override string Texture => ArknightsMod.noTexture;
		public override void SetStaticDefaults() {
		}
		public override void SetDefaults() {
			NPC.width = 1;
			NPC.height = 1;
			NPC.aiStyle = 0;
			NPC.defense = 2147483647;
			NPC.alpha = 0;
			NPC.damage = 0;
			NPC.friendly = false;
			NPC.lifeMax = AACTstg3TH;
			NPC.HitSound = SoundID.NPCHit4;//金属声
			NPC.DeathSound = SoundID.NPCDeath14;//爆炸声
			NPC.knockBackResist = 0f;//击退抗性，0f为最高，1f为最低
			NPC.noGravity = true;
		}

		public int AACTstg3TH {
			get {
				if (!ishealthget) {
					ishealthget = true;
					lasthealth = Main.masterMode ? Main.rand.Next(16, 21) : (Main.expertMode ? Main.rand.Next(12, 17) : Main.rand.Next(8, 13));
					usehealth = lasthealth;
				}
				return lasthealth;
			}
		}

		public int AACTDeathTH {
			get {
				return usehealth;
			}
		}

		private bool ishealthget = false;
		private int lasthealth = 100;
		private int usehealth = 100;
		private int cooldown = 600;
		private bool iscooldownsoundplayed = false;
		private int timer;
		private int randt = 1;
		private int deathtimer;

		//隐藏下方血条
		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) {
			return false;
		}
		//无敌帧
		public override bool? CanBeHitByItem(Player player, Item item) {
			return cooldown <= 0 ? null : false;
		}
		//无敌帧
		public override bool? CanBeHitByProjectile(Projectile Projectile) {
			if (Projectile.hostile == true) {
				return false;
			}
			else if (Projectile.friendly == true) {
				return cooldown <= 0 ? null : false;
			}
			else {
				return false;
			}
		}
		//血量修改
		public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers) {
			Player Player = Main.player[NPC.target < Main.maxPlayers ? NPC.target : 0];
			SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/AACTShield") with { Volume = 1f, Pitch = 0f }, Player.Center);
			iscooldownsoundplayed = true;
			cooldown = Main.rand.Next(60, 121);
			usehealth -= 1;
			NPC.life = usehealth;
		}
		//血量修改
		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) {
			Player Player = Main.player[NPC.target < Main.maxPlayers ? NPC.target : 0];
			SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sound/ImperialArtilleyCoreTargeteer/AACTSTG3HIT") with { Volume = 1f, Pitch = 0f }, Player.Center);
			iscooldownsoundplayed = true;
			cooldown = Main.rand.Next(60, 121);
			usehealth -= 1;
			NPC.life = usehealth;
		}
		public int Host {
			get => (int)NPC.ai[0] - 1;
			set => NPC.ai[0] = value + 1;
		}

		public bool IsAACTActive => Host > -1;

		public static int HostType() {
			return NPCType<AACT>();
		}

		public override void AI() {
			Player Player = Main.player[NPC.target];
			//var AACT = ModContent.GetModNPC(ModContent.NPCType<AACT>());
			NPC HostNPC = Main.npc[Host];
			NPC.Center = HostNPC.Center;
			if (cooldown <= 0) {
				NPC.dontTakeDamage = false;
				if (iscooldownsoundplayed == true) {
					SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/AACTTeleport") with { Volume = 1f, Pitch = 0f }, Player.Center);
					iscooldownsoundplayed = false;
				}
			}
			else {
				cooldown--;
				NPC.dontTakeDamage = true;
			}

			timer++;
			if (timer > 3) {
				randt = Main.rand.Next(2, 31);
				timer = 0;
			}

			if (NPC.life <= 1) {
				deathtimer++;
				NPC.checkDead();
			}
		}

		public override bool CheckDead()//锁血及锁血解除
		{
			if (deathtimer > 15) {
				return true;
			}
			else {
				NPC.life = 1;
				return false;
			}
		}
	}

	public partial class IACTStage3target : global::ArknightsMod.Common.Networking.LegacyEnemyNetwork
	{
		public override string Texture => ArknightsMod.noTexture;
		public override void SetStaticDefaults() {
			Main.npcFrameCount[NPC.type] = 1;//贴图帧数
		}

		public override void SetDefaults() {
			NPC.lifeMax = 1;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.knockBackResist = 0f;//击退抗性，0f为最高，1f为最低
			NPC.width = 1;
			NPC.height = 1;
			NPC.noGravity = true;//无引力
			NPC.noTileCollide = true;//不与物块相撞
			NPC.lavaImmune = true;//免疫岩浆
			NPC.aiStyle = -1;
			NPC.HitSound = SoundID.NPCHit4;//金属声
			NPC.DeathSound = SoundID.NPCDeath14;//爆炸声
		}

		public int ParentIndex {
			get => (int)NPC.ai[0] - 1;
			set => NPC.ai[0] = value + 1;
		}

		public static int BodyType() {
			return NPCType<AACT>();
		}

		private int timer;
		private int stage3atkloop;

		public override void AI() {
			var newSource = NPC.GetSource_FromThis();

			if (Main.masterMode) {
				stage3atkloop = 450;
			}
			else if (Main.expertMode) {
				stage3atkloop = 540;
			}
			else {
				stage3atkloop = 630;
			}
			if (timer % stage3atkloop == 0) {
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, NPC.Center.X, NPC.Center.Y, 0, 0, ProjectileType<ISTG3InCore>(), 0, 0f, 0, NPC.whoAmI);
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, NPC.Center.X, NPC.Center.Y, 0, 0, ProjectileType<ISTG3InRing>(), 0, 0f, 0, NPC.whoAmI);
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, NPC.Center.X, NPC.Center.Y, 0, 0, ProjectileType<ISTG3OutPrism>(), 0, 0f, 0, NPC.whoAmI);
				global::ArknightsMod.Common.Networking.EnemyAuthority.NewProjectile(newSource, NPC.Center.X, NPC.Center.Y, 0, 0, ProjectileType<ISTG3OutRing>(), 0, 0f, 0, NPC.whoAmI);
			}
			timer++;
			Player Player = Main.player[NPC.target];
			NPC parentNPC = Main.npc[ParentIndex];
			NPC.dontTakeDamage = true;
			NPC.Center = 2 * Player.Center - parentNPC.Center;
			if (parentNPC.life == 1 || parentNPC.active == false) {
				die();
			}
		}

		private int killtimer;

		private void die() {
			killtimer++;
			NPC.alpha = 4 * killtimer;
			if (NPC.alpha > 255) {
				NPC.alpha = 255;
			}
			NPC.scale = 1 - killtimer / 60;
			if (NPC.scale < 0) {
				NPC.scale = 0;
			}
			if (killtimer >= 120) {
				NPC.dontTakeDamage = false;
				NPC.life = 0;
			}
		}
	}

	public partial class ISTG3InCore : ModProjectile
	{
		private const string ChainTextPath = "ArknightsMod/Content/NPCs/Enemy/RoaringFlare/ImperialArtilleyCoreTargeteer/ISTG3InCore";

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			if (Main.masterMode) {
				Projectile.timeLeft = 360;
			}
			else if (Main.expertMode) {
				Projectile.timeLeft = 450;
			}
			else {
				Projectile.timeLeft = 540;
			}
			Projectile.alpha = 10;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		private int timer;

		public int ParentIndex {
			get => (int)Projectile.ai[0] - 1;
			set => Projectile.ai[0] = value + 1;
		}

		public override void AI() {
			timer++;
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			NPC parentNPC = Main.npc[ParentIndex];
			Projectile.Center = 2 * Player.Center - parentNPC.Center;
			if (Main.masterMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 60 - timer * 6 + 255);
				if ((timer >= 0 && timer <= 60) || (timer >= 300 && timer <= 360)) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 60 - Math.PI / 2) + 0.9f;
				}
			}
			else if (Main.expertMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 75 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else if (timer >= 60 && timer <= 390) {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 70 - 5 * Math.PI / 14) + 0.9f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 + Math.PI / 2);
				}
			}
			else {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 90 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else if (timer >= 60 && timer <= 480) {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 70 - 5 * Math.PI / 14) + 0.9f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 - 3 * Math.PI / 4);
				}
			}
			if (Projectile.alpha < 0) {
				Projectile.alpha = 0;
			}
		}
	}

	public partial class ISTG3InRing : ModProjectile
	{
		private const string ChainTextPath = "ArknightsMod/Content/NPCs/Enemy/RoaringFlare/ImperialArtilleyCoreTargeteer/ISTG3InRing";

		public override void SetDefaults() {
			Projectile.width = 52;
			Projectile.height = 52;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			if (Main.masterMode) {
				Projectile.timeLeft = 360;
			}
			else if (Main.expertMode) {
				Projectile.timeLeft = 450;
			}
			else {
				Projectile.timeLeft = 540;
			}
			Projectile.alpha = 10;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		private int timer;

		public int ParentIndex {
			get => (int)Projectile.ai[0] - 1;
			set => Projectile.ai[0] = value + 1;
		}

		public override void AI() {
			timer++;
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			NPC parentNPC = Main.npc[ParentIndex];
			Projectile.Center = 2 * Player.Center - parentNPC.Center;
			Projectile.rotation = -0.05f * timer;
			if (Main.masterMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 60 - timer * 6 + 255);
				if ((timer >= 0 && timer <= 60) || (timer >= 300 && timer <= 360)) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 60 - Math.PI / 2) + 0.9f;
				}
			}
			else if (Main.expertMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 75 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else if (timer >= 60 && timer <= 390) {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 70 - 5 * Math.PI / 14) + 1f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 + Math.PI / 2);
				}
			}
			else {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 90 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else if (timer >= 60 && timer <= 480) {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 70 - 5 * Math.PI / 14) + 0.9f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 - 3 * Math.PI / 4);
				}
			}
			if (Projectile.alpha < 0) {
				Projectile.alpha = 0;
			}
		}
	}

	public partial class ISTG3OutRing : ModProjectile
	{
		private const string ChainTextPath = "ArknightsMod/Content/NPCs/Enemy/RoaringFlare/ImperialArtilleyCoreTargeteer/ISTG3OutRing";

		public override void SetDefaults() {
			Projectile.width = 166;
			Projectile.height = 166;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			if (Main.masterMode) {
				Projectile.timeLeft = 360;
			}
			else if (Main.expertMode) {
				Projectile.timeLeft = 450;
			}
			else {
				Projectile.timeLeft = 540;
			}
			Projectile.alpha = 10;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		private int timer;

		public int ParentIndex {
			get => (int)Projectile.ai[0] - 1;
			set => Projectile.ai[0] = value + 1;
		}

		public override void AI() {
			timer++;
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			NPC parentNPC = Main.npc[ParentIndex];

			Projectile.Center = 2 * Player.Center - parentNPC.Center;
			Projectile.rotation = 0.05f * timer;
			if (Main.masterMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 60 - timer * 6 + 255);
				if ((timer >= 0 && timer <= 60) || (timer >= 300 && timer <= 360)) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else {
					Projectile.scale = 1f;
				}
			}
			else if (Main.expertMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 75 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else if (timer >= 60 && timer <= 390) {
					Projectile.scale = 1f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 + Math.PI / 2);
				}
			}
			else {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 90 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120);
				}
				else if (timer >= 60 && timer <= 480) {
					Projectile.scale = 1f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 - 3 * Math.PI / 4);
				}
			}
			if (Projectile.alpha < 0) {
				Projectile.alpha = 0;
			}
		}
	}

	public partial class ISTG3OutPrism : ModProjectile
	{
		private const string ChainTextPath = "ArknightsMod/Content/NPCs/Enemy/RoaringFlare/ImperialArtilleyCoreTargeteer/ISTG3OutPrism";

		public override void SetDefaults() {
			Projectile.width = 132;
			Projectile.height = 132;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			if (Main.masterMode) {
				Projectile.timeLeft = 360;
			}
			else if (Main.expertMode) {
				Projectile.timeLeft = 450;
			}
			else {
				Projectile.timeLeft = 540;
			}
			Projectile.alpha = 10;
			Projectile.damage = 0;
			Projectile.light = 0.6f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		private int timer;

		public int ParentIndex {
			get => (int)Projectile.ai[0] - 1;
			set => Projectile.ai[0] = value + 1;
		}

		public override void AI() {
			timer++;
			Player Player = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
			NPC parentNPC = Main.npc[ParentIndex];
			Projectile.Center = 2 * Player.Center - parentNPC.Center;
			Projectile.rotation = 0.05f * timer;
			if (Main.masterMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 60 - timer * 6 + 255);
				if ((timer >= 0 && timer <= 60) || (timer >= 300 && timer <= 360)) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120) + 0.2f;
				}
				else {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 60 - Math.PI / 2) + 1.1f;
				}
			}
			else if (Main.expertMode) {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 75 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120) + 0.2f;
				}
				else if (timer >= 60 && timer <= 390) {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 70 - 5 * Math.PI / 14) + 1.1f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 + Math.PI / 2) + 0.2f;
				}
			}
			else {
				Projectile.alpha = (int)(Math.Pow(timer, 2) / 90 - timer * 6 + 255);
				if (timer >= 0 && timer <= 60) {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120) + 0.2f;
				}
				else if (timer >= 60 && timer <= 480) {
					Projectile.scale = 0.1f * (float)Math.Sin(timer * Math.PI / 70 - 5 * Math.PI / 14) + 1.1f;
				}
				else {
					Projectile.scale = (float)Math.Sin(timer * Math.PI / 120 - 3 * Math.PI / 4) + 0.2f;
				}
			}
			if (Projectile.alpha < 0) {
				Projectile.alpha = 0;
			}
		}
	}

	public partial class AACTEndShow : ModProjectile
	{
		public override string Texture => ArknightsMod.noTexture;
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 600;
		}

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 3200;
			Projectile.alpha = 0;
			Projectile.damage = 0;
			Projectile.light = 0f;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.scale = 1f;
		}

		public int ParentIndex {
			get => (int)Projectile.ai[0] - 1;
			set => Projectile.ai[0] = value + 1;
		}

		public static int HostType() {
			return NPCType<AACT>();
		}

		private float timer;
		private int colortimer;
		private int r;
		private int g;

		public override void AI() {
			timer++;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC SeekForNPCs = Main.npc[i];
				if (SeekForNPCs.active && SeekForNPCs.type == NPCType<AACT>()) {
					Projectile.Center = SeekForNPCs.Center;
					colortimer += Math.Min(Math.Max(1, (int)(16 * timer / Projectile.timeLeft)), Projectile.timeLeft / 10);
				}
			}

			if (colortimer < 240) {
				r = 0;
				g = 240 - colortimer;
			}
			else if (colortimer < 440) {
				r = colortimer - 240;
				g = 0;
			}
			else if (colortimer < 640) {
				r = 640 - colortimer;
				g = 0;
			}
			else if (colortimer < 880) {
				r = 0;
				g = colortimer - 640;
			}
			else {
				colortimer = 0;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D trailtexture = Request<Texture2D>("ArknightsMod/Common/VisualEffects/LineTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color(r, g, 240), new Color(0, 0, 0), 25f * Math.Min(timer / 600, 1), true);
			return true;
		}
	}
}
