using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items;
using ArknightsMod.Content.Projectiles;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using static Terraria.ModLoader.ModContent;

namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4
{
	public class CrossbowmanLeader : ModNPC
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 19;

			NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() {
				Velocity = 1f
			};
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}

		public override void SetDefaults() {
			NPC.lifeMax = 75;
			NPC.damage = 10;
			NPC.defense = 12;
			NPC.knockBackResist = 0.5f;//击退抗性，0f为最高，1f为最低
			NPC.width = 32;
			NPC.height = 56;
			NPC.aiStyle = 0;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath2;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath2;
			NPC.value = 60f;
			NPC.friendly = false;

			var genreNPC = NPC.GetGlobalNPC<DamageCategoryNPC>();
			genreNPC.artsResistance = 0.05f;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {

			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Material.Device>(), 8, 1, 1));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Material.Polyketon>(), 8, 1, 1));

		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			if (NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 || NPC.downedQueenBee || Main.hardMode) {
				return SpawnCondition.OverworldNightMonster.Chance * 0.01f;
			}
			else {
				return 0f;
			}
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.ArknightsMod.Bestiary.Crossbowman")),
			});
		}

		public override bool? CanBeHitByItem(Player player, Item item)//无敌帧
		{
			return null;
		}

		public override bool? CanBeHitByProjectile(Projectile Projectile)//不被敌方弹幕和无来源弹幕攻击&闪避
		{
			if (Projectile.hostile == true) {
				return false;
			}
			else if (Projectile.friendly == true) {
				return null;
			}
			else {
				return false;
			}
		}

		public override void HitEffect(NPC.HitInfo hit) {
			for (int i = 0; i < 10; i++) {
				int dustType = DustID.RedMoss;
				var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType);
				dust.velocity.X += Main.rand.NextFloat(-0.05f, 0.05f);
				dust.velocity.Y += Main.rand.NextFloat(-0.05f, 0.05f);
				dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);
			}
		}

		private float timer = 180;
		private int atkloop = 180;
		private int atkrange = 320;
		private int escrange = 160;
		private bool iswalk = false;
		private bool isatk = false;
		private bool isescape = false;
		private bool isstuck = false;
		private int walkframe;
		private int atkframe;
		private int framecount;
		private int jumpstage;
		private float jumpspeed;
		private int jumptimes;
		private float distance;
		private float diffX;
		private float diffY;
		private float ax;
		private float vx;
		private float escapetimer = 0;
		private float escapetime;
		private int attacktimer;
		private float angle;
		private int directionchoose;

		public override void AI() {
			Player Player = Main.player[NPC.target];
			diffX = Player.Center.X - NPC.Center.X;
			diffY = Player.Center.Y - NPC.Center.Y;
			ax = 0.2f;
			distance = (float)Math.Sqrt(Math.Pow(diffX / 16, 2) + Math.Pow(diffY / 16, 2));//到玩家的距离（格数）
			if (NPC.velocity.X != 0) {
				NPC.spriteDirection = Math.Sign(NPC.velocity.X);
				NPC.rotation = 0;
			}
			if (Main.masterMode) {
				atkloop = 90;
				atkrange = 30;
				escrange = 15;
				jumpspeed = 6f;
				escapetime = 300;
				vx = 1.5f;
			}
			else if (Main.expertMode) {
				atkloop = 120;
				atkrange = 20;
				escrange = 10;
				jumpspeed = 5f;
				escapetime = 240;
				vx = 1.2f;
			}
			else {
				atkloop = 180;
				atkrange = 15;
				escrange = 10;
				jumpspeed = 4f;
				escapetime = 180;
				vx = 1f;
			}
			//玩家死亡
			if (!Player.active || Player.dead) {
				NPC.TargetClosest(false);
				if (Player.dead) {
					Escape();
					return;
				}
			}

			//模式选择
			if (distance >= atkrange) {//攻击范围之外
				Walk();
			}
			else if (distance >= escrange + 2) {//逃跑范围之外
				timer++;
				if (timer >= atkloop) {
					Attack();
					//timer = 0;
				}
				else {
					Walk();
					//attacktimer = 0;
				}
			}
			else if (distance >= escrange) {//中间范围，只攻击不移动
				timer++;
				NPC.velocity.X = float.Lerp(NPC.velocity.X, 0, 0.1f);
				if (timer >= atkloop) {
					Attack();
					//timer = 0;
				}
				else {
					//attacktimer = 0;
				}
			}
			else {//逃跑范围之内
				timer++;
				if (timer >= atkloop) {
					Attack();
					//timer = 0;
				}
				else {
					Escape();
					//attacktimer = 0;
				}
			}
		}

		private void Walk() {//靠近，如果横向速度为0时纵向速度也为0，尝试跳跃，跳跃数次后反方向逃走
			Player Player = Main.player[NPC.target];
			iswalk = true;
			isatk = false;
			isescape = false;
			if (NPC.velocity.X == 0) {
				isstuck = true;
				Vector2 velDiff = NPC.velocity - Player.velocity;
				int haltDirectionX = velDiff.X > 0 ? 1 : -1;
				float haltPointX = NPC.Center.X + haltDirectionX * (velDiff.X * velDiff.X) / (2 * ax);
				if (Player.Center.X > haltPointX) {
					NPC.velocity.X += ax;
				}
				else {
					NPC.velocity.X -= ax;
				}
				NPC.velocity.X = Math.Min(vx, Math.Max(-vx, NPC.velocity.X));
				if (NPC.velocity.Y == 0) {
					jumpstage += 1;
					if (jumpstage >= 1) {
						NPC.velocity.Y -= jumpspeed;
						jumpstage = 0;
						jumptimes += 1;
						if (jumptimes >= 3) {
							jumptimes = 0;
							//escapetimer = 0;
							Escape();
							return;
						}
					}
				}
			}
			else {
				isstuck = false;
				jumptimes = 0;
				jumpstage = 0;
				Vector2 velDiff = NPC.velocity - Player.velocity;
				int haltDirectionX = velDiff.X > 0 ? 1 : -1;
				float haltPointX = NPC.Center.X + haltDirectionX * (velDiff.X * velDiff.X) / (2 * ax);
				if (Player.Center.X > haltPointX) {
					NPC.velocity.X += ax;
				}
				else {
					NPC.velocity.X -= ax;
				}
				NPC.velocity.X = Math.Min(vx, Math.Max(-vx, NPC.velocity.X));
				return;
			}
		}
		private int jumpCD;
		private void Escape() {//往远离玩家方向走，可选择攻击或不攻击
			escapetimer++;
			isescape = true;
			iswalk = false;
			isatk = false;
			isstuck = false;
			if (escapetimer <= escapetime) {
				jumpCD++;
				Player Player = Main.player[NPC.target];
				Vector2 velDiff = NPC.velocity - Player.velocity;
				int haltDirectionX = velDiff.X > 0 ? 1 : -1;
				float haltPointX = NPC.Center.X + haltDirectionX * (velDiff.X * velDiff.X) / (2 * ax);
				if (Player.Center.X > haltPointX) {
					NPC.velocity.X -= ax;
				}
				else {
					NPC.velocity.X += ax;
				}
				NPC.velocity.X = Math.Min(vx, Math.Max(-vx, NPC.velocity.X));
				if (NPC.velocity.Y == 0) {
					jumpstage += 1;

					if (jumpstage >= 1 && jumpCD >= 60) {
						NPC.velocity.Y -= jumpspeed;
						jumpstage = 0;
						jumpCD = 0;
						jumptimes += 1;
						if (jumptimes >= 3) {
							jumptimes = 0;
							Walk();
							return;
						}
					}
				}
			}
			else {
				escapetimer = 0;
				return;
			}
		}

		private void Attack() {
			var newSource = NPC.GetSource_FromThis();
			Player Player = Main.player[NPC.target];
			attacktimer++;
			if (attacktimer <= 30) {
				NPC.velocity.X = float.Lerp(NPC.velocity.X, 0, 0.1f);
			}
			else if (attacktimer <= 70) {
				//NPC.velocity = Vector2.Zero;
				isatk = true;
				iswalk = false;
				isescape = false;
				isstuck = false;
				if (attacktimer == 50) {
					directionchoose = Player.Center.X - NPC.Center.X >= 0 ? 1 : -1;
					angle = (float)Math.Atan((Player.Center.Y - NPC.Center.Y) / (Player.Center.X - NPC.Center.X));
					Projectile.NewProjectile(newSource, NPC.Center, new Vector2(directionchoose * 8f, 0).RotatedBy(angle), ModContent.ProjectileType<CrossbowmanBolt>(), 16, 0.8f, 0, 0);
					SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/Crossbow") with { Volume = 1f, Pitch = 0f }, NPC.Center);
				}
			}
			else {
				isatk = false;
				isstuck = true;
				timer = 0;
				attacktimer = 0;
				return;
			}
		}

		public override void FindFrame(int frameHeight) {
			frameHeight = 56;
			if (iswalk == true || isescape == true) {
				walkframe++;
				framecount = (int)(walkframe / 20);
				if (framecount > 13) {
					walkframe = 0;
				}
				NPC.frame.Y = framecount * frameHeight;
			}
			if (isstuck == true) {
				NPC.frame.Y = 14 * frameHeight;
			}
			if (isatk == true) {
				atkframe++;
				framecount = (int)(atkframe / 10) + 15;
				NPC.frame.Y = framecount * frameHeight;
				if (framecount > 18) {
					NPC.frame.Y = 15 * frameHeight;
					atkframe = 0;
				}
			}
		}
		public override void OnKill() {
			SoundStyle ghostSound = SoundID.NPCDeath7 with {
				Pitch = -0.4f, // 范围[-1.0, 1.0]，-0.5表示降低八度
				Volume = 0.6f  // 可选调整音量
			};
			SoundEngine.PlaySound(ghostSound, NPC.Center);
			for (int i = 0; i < 25; i++) // 总粒子数
	{
				// 70%概率生成黑色，30%概率生成橙色
				bool isBlack = Main.rand.NextFloat() < 0.7f;

				Dust dust = Dust.NewDustPerfect(
					NPC.Center,
					isBlack ? DustID.Asphalt : DustID.FireworksRGB, // 黑色或橙色
					Main.rand.NextVector2Circular(5, 5),
					Alpha: 150,
					Scale: Main.rand.NextFloat(1.2f, 2f)
				);

				// 统一物理参数
				dust.noGravity = true;
				dust.fadeIn = 1.5f;
				dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
			}
		}
		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) {
			

				for (int i = 0; i < 3; i++) {
					Dust.NewDust(NPC.position, NPC.width, NPC.height,
						DustID.MagicMirror, 0, 0, 150, Color.LightBlue, 0.7f);
				}
			
		}
	}
}
	