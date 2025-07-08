using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using System;
using Microsoft.Xna.Framework.Graphics;
using ArknightsMod.Common.VisualEffects;
using Terraria.DataStructures;
using Terraria.Localization;

namespace ArknightsMod.Content.NPCs.Enemy.OF.Pmp
{
    public class Pompeii : ModNPC
    {
        private enum AIState
        {
            Mode1Crawl,
            Mode2ShootFireballs,
            Mode3Idle,
            Mode4RainFire,
            Mode5Idle,
            TailKillRise,
            TailKillSpin,
            TailKillDeath
        }

        private AIState _currentState = AIState.Mode1Crawl;
        private int _modeTimer;
        private int _explosionCooldown;
        private int _tailKillDuration;
        private bool _hasEnteredTailKill;
        private Vector2 _tailKillStartPos;
        private int _fireRainCounter;
        private int _currentFrame; //当前帧
        private int _frameCounter; 
        private const int FrameSpeed = 5;
        private int _frameResetFlag;
		private float escapetimer;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 53;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.ShouldBeCountedAsBoss[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 176;
            NPC.height = 154;
            NPC.lifeMax = 4000;
            NPC.defense = 15;
            NPC.damage = 160;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.lavaImmune = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.aiStyle = -1;
			Music = MusicLoader.GetMusicSlot(Mod, "Music/PmpBoss");
		}

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			target.buffImmune[BuffID.OnFire] = false;
			if (!target.HasBuff(BuffID.OnFire)) {
				target.AddBuff(BuffID.OnFire, Main.masterMode ? 180 : Main.expertMode ? 180 : 270);
			}
		}

		public override bool CheckDead() //尾杀
        {
            if (!_hasEnteredTailKill)
            {
                InitTailKill();
                return false;
            }
            return true;
        }

        public override void AI()
        {
            NPC.TargetClosest();
            Player player = Main.player[NPC.target];
			Lighting.AddLight(NPC.Center, 10);
			#region 玩家死亡后消失
			if (!player.active || player.dead || (player.Center - NPC.Center).Length() > Main.screenWidth) {
				NPC.TargetClosest(false);
				player = Main.player[NPC.target];
				escapetimer++;
				if (escapetimer > 50) {
					for (int i = 0; i < 3; i++) {
						Main.dust[Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Pixie, Scale: 1.5f)].noGravity = true;
					}
					for (int j = 0; j < 6; j++) {
						Main.dust[Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, Scale: 2.5f)].noGravity = true;
					}
				}
				if (escapetimer > 60) {
					NPC.Center = Vector2.Zero;
					NPC.active = false;
				}
				return;
			}
			#endregion

			ExecuteStateMachine(player);
            UpdateAnimation(); 
        }

        private void ExecuteStateMachine(Player player)
        {
            switch (_currentState)
            {
                case AIState.Mode1Crawl: Mode1Crawl(player); break;
                case AIState.Mode2ShootFireballs: Mode2ShootFireballs(player); break;
                case AIState.Mode3Idle: Mode3Idle(); break;
                case AIState.Mode4RainFire: Mode4RainFire(player); break;
                case AIState.Mode5Idle: Mode5Idle(); break;
                case AIState.TailKillRise: HandleTailKillRise(); break;
                case AIState.TailKillSpin: HandleTailKillSpin(player); break;
                case AIState.TailKillDeath: HandleTailKillDeath(); break;
            }

            //if (!_hasEnteredTailKill)
            NPC.direction = NPC.spriteDirection = (player.Center.X > NPC.Center.X) ? -1 : 1;
        }

		#region 帧图与绘制
		private void UpdateAnimation()
        {
            if (_frameResetFlag > 0)
            {
                _currentFrame = GetStateMinFrame(_currentState);
                _frameCounter = 0;
                _frameResetFlag--;
            }

            _frameCounter++;
            if (_frameCounter < FrameSpeed) return;

            _frameCounter = 0;
            int minFrame = GetStateMinFrame(_currentState);
            int maxFrame = GetStateMaxFrame(_currentState);
            //if (_currentState == AIState.TailKillSpin) return;
            if (_currentState == AIState.TailKillDeath && _currentFrame >= maxFrame) return;
            if (_currentFrame < minFrame || _currentFrame >= maxFrame)
                _currentFrame = minFrame;
            else
                _currentFrame++;
        }

        private int GetStateMinFrame(AIState state)
        {
            return state switch
            {
                AIState.Mode1Crawl => 26,
                AIState.Mode2ShootFireballs => 1,
                AIState.Mode3Idle => 19,
                AIState.Mode4RainFire => 8,
                AIState.Mode5Idle => 19,
                AIState.TailKillRise => 1,
                AIState.TailKillSpin => 1,
                AIState.TailKillDeath => 11,
                _ => 0
            };
        }

        private int GetStateMaxFrame(AIState state)
        {
            return state switch
            {
                AIState.Mode1Crawl => 35,
                AIState.Mode2ShootFireballs => 10,
                AIState.Mode3Idle => 25,
                AIState.Mode4RainFire => 11,
                AIState.Mode5Idle => 25,
                AIState.TailKillRise => 10,
                AIState.TailKillSpin => 11,
                AIState.TailKillDeath => 17,
                _ => Main.npcFrameCount[Type]
            };
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frame = new Rectangle(0, _currentFrame * frameHeight, NPC.width, frameHeight);
        }

		//发光图层
		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D PmpGlow = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/OF/Pmp/Pompeii_glow").Value;
			Main.EntitySpriteDraw(PmpGlow, NPC.Center - Main.screenPosition + new Vector2(0, 3) + new Vector2(0, (_currentFrame * PmpGlow.Height / 53 / 2)).RotatedBy(NPC.rotation), new Rectangle(0, (int)(_currentFrame * PmpGlow.Height / 53f), PmpGlow.Width, PmpGlow.Height / 53), Color.White, NPC.rotation, new Vector2(PmpGlow.Width / 2, (_currentFrame + 1) * (PmpGlow.Height / 53) / 2), 1f, NPC.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
		}
		#endregion

		private void Mode1Crawl(Player player)
        {
			float timecount = Main.masterMode ? 3 : Main.expertMode ? 4.5f : 6; //爬行时间
			float ax = 0.1f;
			float vx = Main.masterMode ? 3.4f : Main.expertMode ? 3.0f : 2.6f; //最大速度
			int direction = (Main.player[NPC.target].Center.X > NPC.Center.X).ToDirectionInt();
			NPC.direction = direction;
			Vector2 velDiff = NPC.velocity - player.velocity;
			int haltDirectionX = velDiff.X > 0 ? 1 : -1;
			float haltPointX = NPC.Center.X + haltDirectionX * (velDiff.X * velDiff.X) / (2 * ax);

			if (player.Center.X > haltPointX) {
				NPC.velocity.X += ax;
			}
			else {
				NPC.velocity.X -= ax;
			}
			NPC.velocity.X = Math.Min(vx, Math.Max(-vx, NPC.velocity.X));

			if (++_modeTimer >= 60 * timecount)
            {
                _currentState = AIState.Mode2ShootFireballs;
                _modeTimer = 0;
                _frameResetFlag = 2; 
            }
        }

        private void Mode2ShootFireballs(Player player)
        {

			NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.1f);
			int shootnum = Main.masterMode ? Main.rand.Next(6, 10) : Main.expertMode ? Main.rand.Next(4, 7) : Main.rand.Next(3, 6);
            if (_modeTimer == 20)
            {
                for (int i = 0; i < shootnum; i++)
                {
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(55f * NPC.direction, -35f), Vector2.Zero, ModContent.ProjectileType<PmpFireBall>(), 120, 6f, Main.myPlayer);
                }
                SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
            }

			if ((Main.expertMode || Main.masterMode) && _modeTimer == 40) {
				for (int i = 0; i < shootnum; i++) {
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(55f * NPC.direction, -35f), Vector2.Zero, ModContent.ProjectileType<PmpFireBall>(), 120, 6f, Main.myPlayer);
				}
				SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
			}

            if (++_modeTimer >= 60)
            {
                _currentState = AIState.Mode3Idle;
                _modeTimer = 0;
                _frameResetFlag = 2;
            }
        }

        private void Mode3Idle()
        {
			//float timecount = Main.masterMode ? 2 : Main.expertMode ? 2.5f : 3;
			float timecount = 3;

			if (_modeTimer >= 90 && _modeTimer <= 105) {
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(-25f * NPC.direction, -70f), Vector2.Zero, ModContent.ProjectileType<PmpFireRain>(), 0, 0, Main.myPlayer, 0);
			}
			if (_modeTimer == 90) {
				SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
			}

			if (++_modeTimer >= 60 * timecount)
            {
                _currentState = AIState.Mode4RainFire;
                _modeTimer = 0;
                _fireRainCounter = 0;
                _frameResetFlag = 2;
            }
        }

        private void Mode4RainFire(Player player)
        {
			int skilltime = Main.masterMode ? 60 : Main.expertMode ? 75 : 90;
			int modetime = Main.masterMode ? 180 : Main.expertMode ? 225 : 270;
			if (_modeTimer % 60 == 0 && _fireRainCounter < 3)
            {
                int fireballCount = Main.masterMode ? Main.rand.Next(8, 13) : Main.expertMode ? Main.rand.Next(6, 10) : Main.rand.Next(4, 7);
                for (int i = 0; i < fireballCount; i++)
                {
                    Vector2 spawnPos = new Vector2(player.Center.X + Main.rand.NextFloat(-600, 600), player.Center.Y + Main.rand.NextFloat(-120, 120) - 600);
					Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, Vector2.Zero, ModContent.ProjectileType<PmpFireRain>(), 75, 6f, Main.myPlayer, 1);
                }
                _fireRainCounter++;
                SoundEngine.PlaySound(SoundID.Item34, NPC.Center);
            }

            if (_fireRainCounter >= 3 && _modeTimer > 180)
            {
                _currentState = AIState.Mode5Idle;
                _modeTimer = 0;
                _frameResetFlag = 2;
            }
            else _modeTimer++;
        }

        private void Mode5Idle()
        {
			float timecount = Main.masterMode ? 1.5f : Main.expertMode ? 2 : 2.5f;

			if (++_modeTimer >= 60 * timecount)
            {
                _currentState = AIState.Mode1Crawl;
                _modeTimer = 0;
                _frameResetFlag = 2;
            }
        }

        private void InitTailKill()
        {
            _hasEnteredTailKill = true;
            _currentState = AIState.TailKillRise;
            _tailKillDuration = 0;
            _tailKillStartPos = NPC.Center;
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            NPC.velocity = Vector2.Zero;
            _frameResetFlag = 2;
        }

        private void HandleTailKillRise()
        {
            _tailKillDuration++;
            NPC.position.Y -= 40f / 60f;

			if (_tailKillDuration >= 90 && _tailKillDuration % 48 == 0) {
				for (int i = 0; i <= Main.rand.Next(3,7); i++) {
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(-25f * NPC.direction, -70f), Vector2.Zero, ModContent.ProjectileType<PmpFireRain>(), 0, 0, Main.myPlayer, 0);
				}
				SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
			}

			if (_tailKillDuration >= 60 * 4)
            {
                _currentState = AIState.TailKillSpin;
                _tailKillDuration = 0;
            }
        }

        private void HandleTailKillSpin(Player player)
        {
            _tailKillDuration++;
			int shootdelta = Main.masterMode ? 48 : Main.expertMode ? 60 : 75;
			int shootnum = Main.masterMode ? Main.rand.Next(6, 10) : Main.expertMode ? Main.rand.Next(4, 7) : Main.rand.Next(3, 6);
			int shoottime = Main.masterMode ? 32 : Main.expertMode ? 28 : 24;
			if (_tailKillDuration % shootdelta == 0)
            {
				if (_tailKillDuration <= (shoottime - 1) * shootdelta) {
					for (int j = 0; j <= Main.rand.Next(3, 7); j++) {
						Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(-25f * NPC.direction, -70f), Vector2.Zero, ModContent.ProjectileType<PmpFireRain>(), 0, 0, Main.myPlayer, 0);
					}
				}

				for (int i = 0; i < shootnum; i++)
                {
					Vector2 spawnPos = new Vector2(NPC.Center.X + Main.rand.NextFloat(-600, 600), NPC.Center.Y + Main.rand.NextFloat(-60, 60) - 540);
					Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, Vector2.Zero, ModContent.ProjectileType<PmpFireRain>(), 75, 6f, Main.myPlayer, 1);
				}
                SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
            }

            if (_tailKillDuration >= shoottime * shootdelta)
            {
                _currentState = AIState.TailKillDeath;
                _tailKillDuration = 0;
                _frameResetFlag = 2;
            }
        }

		private void HandleTailKillDeath()
        {
			Player Player = Main.player[Main.myPlayer];
			_tailKillDuration++;

			if (_tailKillDuration == 45) {
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<PmpExplode>(), 0, 0, -1);
			}

			if (_tailKillDuration == 60)
            {
                for (int i = 0; i < 100; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.position,NPC.width,NPC.height,DustID.Torch);
                    dust.velocity = Main.rand.NextVector2Circular(20f, 20f);
                    dust.scale = 2.5f;
                    dust.noGravity = true;
                }
                SoundEngine.PlaySound(SoundID.Item14, NPC.Center);

				if ((NPC.Center - Player.Center).Length() <= 240f) {
					Player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.ArknightsMod.StatusMessage.Pmp.GetTooClose", Player.name)), 1018, 0);
				}
			}

            if (_tailKillDuration >= 75)
            {
				NPC.life = 0;
                NPC.checkDead();
            }
		}
    }

	//火球
	public class PmpFireBall : ModProjectile
	{

		public override string Texture => ArknightsMod.noTexture;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 15;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 600;
			Projectile.alpha = 0;
			Projectile.damage = 40;
			//Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 0f;
		}

		private float timer;
		private int r = 255;
		private int g = 50;
		private int b = 0;
		private Vector2 spawnPos;
		private float targethittime;//预计撞击时间
		private float randomay; //随机纵向加速度

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			target.buffImmune[BuffID.OnFire] = false;
			if (!target.HasBuff(BuffID.OnFire)) {
				target.AddBuff(BuffID.OnFire, Main.masterMode ? 180 : Main.expertMode ? 180 : 270);
			}
		}

		public override void OnSpawn(IEntitySource source) {
			spawnPos = Projectile.Center;
			targethittime = Main.masterMode ? Main.rand.NextFloat(48, 72) : Main.expertMode ? Main.rand.NextFloat(60, 90) : Main.rand.NextFloat(75, 105);
			randomay = Main.rand.NextFloat(0.1f, 0.5f);
			g = 50 + Main.rand.Next(0, 81);
		}

		public override void AI() {
			Player Player = Main.player[Main.myPlayer];
			Lighting.AddLight(Projectile.Center, 10);
			Projectile.rotation = Projectile.velocity.ToRotation() + float.Pi / 2;
			Vector2 deltaPos = Player.Center - spawnPos;
			Dust dust = Main.dust[Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, new Color(255, 255, 255), 2f)];
			dust.noGravity = true;
			if (timer <= 30f) {
				Projectile.Opacity = timer / 30f;
				Projectile.scale = timer / 30f;
			}
			float targetvx = deltaPos.X / targethittime + Main.rand.NextFloat(-0.25f, 0.25f) * Player.velocity.X;
			float targetvy = -randomay * targethittime / 2f + randomay * timer;
			Projectile.velocity = new Vector2(targetvx, targetvy);

			timer++;
		}

		public override void PostDraw(Color lightColor) {
			Texture2D text = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/OF/Pmp/PmpFireBall").Value;
			Main.EntitySpriteDraw(text, Projectile.Center - Main.screenPosition - Projectile.velocity * 0.5f, new Rectangle(0, 0, text.Width, text.Height), new Color(r, g, b) * Projectile.Opacity, Projectile.rotation, new Vector2(text.Width / 2, text.Height / 2), 0.4f * Projectile.scale, SpriteEffects.None, 0);
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/LineTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color(r, g, b), new Color(0, 0, 0), 10f, true);
			return true;
		}

		public override void OnKill(int timeLeft) {
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center); //炸弹
			for (int i = 0; i < 3; i++) {
				Main.dust[Dust.NewDust(Projectile.position - new Vector2(10, 10), Projectile.width + 20, Projectile.height + 20, DustID.Pixie, Scale: 1.5f)].noGravity = true;
			}
			for (int j = 0; j < 6; j++) {
				Main.dust[Dust.NewDust(Projectile.position - new Vector2(10, 10), Projectile.width + 20, Projectile.height + 20, DustID.Torch, Scale: 2.5f)].noGravity = true;
			}
		}
	}

	//火雨
	public class PmpFireRain : ModProjectile
	{

		public override string Texture => ArknightsMod.noTexture;

		public override void SetStaticDefaults() {
			//ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 14;
		}

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 600;
			Projectile.alpha = 0;
			Projectile.damage = 60;
			//Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 0f;
		}

		private float timer;
		private int r = 255;
		private int g = 50;
		private int b = 0;
		private Vector2 spawnPos;
		private float vx;
		private float vy;

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			target.buffImmune[BuffID.OnFire] = false;
			if (!target.HasBuff(BuffID.OnFire)) {
				target.AddBuff(BuffID.OnFire, Main.masterMode ? 180 : Main.expertMode ? 180 : 270);
			}
		}

		public override void OnSpawn(IEntitySource source) {
			spawnPos = Projectile.Center;
			vx = Main.rand.NextFloat(-1, 1);
			vy = Main.masterMode ? Main.rand.NextFloat(10.8f, 12f) : Main.expertMode ? Main.rand.NextFloat(9.6f, 11.2f) : Main.rand.NextFloat(8.4f, 10.8f);
			g = 50 + Main.rand.Next(0, 41);
		}

		//存储历史轨迹（方括号内数目比绘制数量大一个deltaT）
		private Vector2[] beforePos = new Vector2[16];

		//修改历史轨迹
		public override void PostAI() {
			//更新缓存数组 （从最大索引开始倒序更新）
			for (int n = 15; n > 0; n--) {
				beforePos[n] = beforePos[n - 1];
			}
			beforePos[0] = Projectile.position + 3 * Projectile.velocity; //按需调整
			//曲线部分（无偏移）曲线部分点数
			for (int i = 0; i < 7; i++) {
				Projectile.oldPos[i] = beforePos[2 * i + (int)timer % 2]; //deltaT
			}
			//折线部分（有偏移）模数为deltaT | 存储轨迹最大索引 | 折线绘制上限索引 | 折线绘制起始索引+1
			if (timer % 2 == 0 && beforePos[15] != Vector2.Zero) {
				//更新
				for (int n = 13; n > 7; n--) {
					Projectile.oldPos[n] = Projectile.oldPos[n - 1];
				}
				Projectile.oldPos[7] = beforePos[15] + new Vector2(0, Main.rand.NextFloat(-24, 24)).RotatedBy(Projectile.velocity.ToRotation()); //第一个索引的引入计算，偏移量垂直于当前速度方向（懒得写旧的速度方向了）
			}
		}

		public override void AI() {
			Lighting.AddLight(Projectile.Center, 10);
			Projectile.rotation = Projectile.velocity.ToRotation() + float.Pi / 2;
			Dust dust;
			Vector2 position = Projectile.Center;
			dust = Main.dust[Dust.NewDust(Projectile.Center + new Vector2(-4, 0), Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, new Color(255, 255, 255), 1.5f)];
			dust.noGravity = true;

			if (timer <= 30f) {
				Projectile.Opacity = timer / 30f;
				Projectile.scale = timer / 30f;
			}

			
			Projectile.velocity = Projectile.ai[0] == 1 ? new Vector2(0, vy) : new Vector2(vx, -vy);

			timer++;
		}

		public override void PostDraw(Color lightColor) {
			Texture2D text = ModContent.Request<Texture2D>("ArknightsMod/Content/NPCs/Enemy/OF/Pmp/PmpFireStar").Value;
			Main.EntitySpriteDraw(text, Projectile.Center - Main.screenPosition - Projectile.velocity * 0.5f, new Rectangle(0, 0, text.Width, text.Height), new Color(r, g, b) * Projectile.Opacity, Projectile.rotation, new Vector2(text.Width / 2, text.Height / 2), 1f * new Vector2(0.25f, 1f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(text, Projectile.Center - Main.screenPosition - Projectile.velocity * 0.5f, new Rectangle(0, 0, text.Width, text.Height), new Color(r, g, b) * Projectile.Opacity, Projectile.rotation + float.Pi / 2, new Vector2(text.Width / 2, text.Height / 2), 1f * new Vector2(0.25f, 1f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(text, Projectile.Center - Main.screenPosition - Projectile.velocity * 0.5f, new Rectangle(0, 0, text.Width, text.Height), new Color(r, g + 120, 150) * Projectile.Opacity, Projectile.rotation, new Vector2(text.Width / 2, text.Height / 2), 0.6f * new Vector2(0.25f, 1f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(text, Projectile.Center - Main.screenPosition - Projectile.velocity * 0.5f, new Rectangle(0, 0, text.Width, text.Height), new Color(r, g + 120, 150) * Projectile.Opacity, Projectile.rotation + float.Pi / 2, new Vector2(text.Width / 2, text.Height / 2), 0.6f * new Vector2(0.25f, 1f), SpriteEffects.None, 0);
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D trailtexture = ModContent.Request<Texture2D>("ArknightsMod/Common/VisualEffects/LineTrail").Value;
			TrailMaker.ProjectileDrawTailByConstWidth(Projectile, trailtexture, Vector2.Zero, new Color(r, g, b), new Color(0, 0, 0), 10f, true);
			return true;
		}

		public override void OnKill(int timeLeft) {
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center); //炸弹
			for (int i = 0; i < 3; i++) {
				Main.dust[Dust.NewDust(Projectile.position - new Vector2(10, 10), Projectile.width + 20, Projectile.height + 20, DustID.Pixie, Scale: 1.5f)].noGravity = true;
			}
			for (int j = 0; j < 6; j++) {
				Main.dust[Dust.NewDust(Projectile.position - new Vector2(10, 10), Projectile.width + 20, Projectile.height + 20, DustID.Torch, Scale: 2.5f)].noGravity = true;
			}
		}
	}

	//爆炸
	public class PmpExplode : ModProjectile
	{

		public override string Texture => ArknightsMod.noTexture;

		public override void SetDefaults() {
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.aiStyle = 0;
			Projectile.penetrate = -1;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 75;
			Projectile.alpha = 0;
			Projectile.damage = 0;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.scale = 0f;
		}

		private int shadertimer = 0;//充当utime
		//private int shadercheck = 0;
		private int ammount = 100;//波纹数量
		private int scalesize = 50;//波纹大小
		private int wavevelocity;//波纹速度
		private float distortStrength = 80f;//强度

		public override void AI() {
			shadertimer++;
			if (shadertimer > 75) {
				shadertimer = 75;
			}
			wavevelocity = 10;
			if (wavevelocity < 0) {
				wavevelocity = 0;
			}

			if (shadertimer <= 60) {
				if (Projectile.ai[0] == 0) {
					Projectile.ai[0] = 1;
					if (Main.netMode != NetmodeID.Server && !Terraria.Graphics.Effects.Filters.Scene["IACTSW"].IsActive()) {
						Terraria.Graphics.Effects.Filters.Scene.Activate("IACTSW", Projectile.Center).GetShader().UseColor(ammount, scalesize, wavevelocity).UseTargetPosition(Projectile.Center);
					}
				}

				if (Main.netMode != NetmodeID.Server && Terraria.Graphics.Effects.Filters.Scene["IACTSW"].IsActive()) {
					float progress = (shadertimer) / 60f;
					Terraria.Graphics.Effects.Filters.Scene["IACTSW"].GetShader().UseProgress(3 * progress).UseOpacity(distortStrength * (1 - progress / 1f));
				}
			}
		}

		public override void OnKill(int timeLeft) {
			if (Main.netMode != NetmodeID.Server && Terraria.Graphics.Effects.Filters.Scene["IACTSW"].IsActive()) {
				Terraria.Graphics.Effects.Filters.Scene["IACTSW"].Deactivate();
			}
		}
	}
}