using ArknightsMod.Content.NPCs.Enemy.W;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// 召唤骰：玩家掷出的十二面骰。落地翻滚、数字乱跳 → 停稳 → 数字锁定 12（她的骰子从来只掷得出 12）<br/>
	/// → 金光聚起 → 炸成一团烟 → 服务端在烟里生成 W（ai0=1：从骰子里登场，走出烟团即可）。<br/>
	/// ai[1]=1 时为「哑弹模式」：W 倒下前掷出的最后一颗——照样停在 12，却只"噗"地冒一缕烟，不炸也不召唤（性格戏）。<br/>
	/// ai[0]=停稳计时（0 未停）；localAI[0]=显示数字；localAI[1]=数字刷新计数；localAI[2]=静止累计。
	/// </summary>
	public class WSummonDie : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/Projectiles/Bosses/W/WD12";

		private const int RestHold = 60;   // 停稳后停留：数字锁定 → 炸
		private const int Timeout = 360;   // 一直没停稳也照炸

		private bool Resting => Projectile.ai[0] > 0f;
		private bool Dud => Projectile.ai[1] == 1f;

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 600;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			if (!Resting) {
				Projectile.velocity.Y += 0.3f;
				if (Projectile.velocity.Y > 14f)
					Projectile.velocity.Y = 14f;
				// 贴地滚动时有摩擦，否则永远停不下来（着地那一帧碰撞会把 Y 归零）
				if (Projectile.velocity.Y == 0f)
					Projectile.velocity.X *= 0.9f;
				Projectile.rotation += Projectile.velocity.X * 0.08f + 0.04f;

				// 数字乱跳（1~11——12 只在停下来那一刻出现）
				if (++Projectile.localAI[1] >= 4f) {
					Projectile.localAI[1] = 0f;
					Projectile.localAI[0] = Main.rand.Next(1, 12);
				}

				bool still = Projectile.velocity.Length() < 0.35f;
				Projectile.localAI[2] = still ? Projectile.localAI[2] + 1f : 0f;
				bool timeout = Projectile.timeLeft < 600 - Timeout;
				// 停稳判定各端各自做：这是玩家所有的弹幕，服务端的 netUpdate 到不了客户端（只有拥有者能推），
				// 靠本地判定客户端才看得到"停稳→锁 12→金光"那一段；真正生成 W 的 Burst 仍只在服务端
				if (Projectile.localAI[2] >= 8f || timeout) {
					Projectile.ai[0] = 1f;
					Projectile.velocity = Vector2.Zero;
				}
				return;
			}

			// 停稳：数字锁 12，金光渐强，每 12 tick 一声递升的响
			if (Projectile.ai[0] == 1f) {
				SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.4f }, Projectile.Center);
				Projectile.rotation = 0f;
			}
			Projectile.ai[0]++;
			Projectile.velocity.X = 0f;
			Projectile.velocity.Y += 0.3f; // 借原版碰撞贴住地面
			Projectile.rotation = 0f;
			Projectile.localAI[0] = 12f;
			float k = Projectile.ai[0] / RestHold;
			if (!Main.dedServ) {
				Lighting.AddLight(Projectile.Center, 1.0f * k, 0.8f * k, 0.3f * k);
				if ((int)Projectile.ai[0] % 12 == 0)
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.4f + k * 0.5f, Volume = 0.6f }, Projectile.Center);
				if (Main.rand.NextBool(2)) {
					Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.GoldFlame,
						new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.6f)), 120, default, Main.rand.NextFloat(0.8f, 1.2f));
					d.noGravity = true;
				}
			}

			if (Projectile.ai[0] >= RestHold && Main.netMode != NetmodeID.MultiplayerClient) {
				if (!Dud)
					Burst();
				Projectile.Kill();
			}
		}

		/// <summary>炸烟 + 生成 W（仅服务端/单机端）</summary>
		private void Burst() {
			// 大号烟团：无主（ai0=-1）→ 2 秒自然消散；W 登场就是从这团烟里走出来
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<WSmokeCloud>(), 0, 0f, Main.myPlayer, ai0: -1f, ai1: 96f);

			int type = ModContent.NPCType<WBoss>();
			if (NPC.AnyNPCs(type))
				return;
			// ai2=1 是一阶段标记（NewNPC 会用参数覆盖 SetDefaults 写的 ai[]）；ai0=1 告诉登场状态"我已在烟里"
			int n = NPC.NewNPC(Projectile.GetSource_FromThis(), (int)Projectile.Center.X, (int)Projectile.Center.Y + 8, type,
				0, ai0: 1f, ai1: 0f, ai2: 1f, ai3: 0f, Target: Projectile.owner);
			if (n < 0 || n >= Main.maxNPCs)
				return;
			NPC npc = Main.npc[n];
			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(Language.GetTextValue("Announcement.HasAwoken", npc.TypeName), 175, 75, 255);
			else if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasAwoken", npc.GetTypeNetName()), new Color(175, 75, 255));
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			// 骰子弹跳：竖直衰减 45%，水平衰减 25%，弹一下响一下
			if (Math.Abs(oldVelocity.Y) > 1.5f) {
				Projectile.velocity.Y = oldVelocity.Y * -0.45f;
				SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
			}
			else {
				Projectile.velocity.Y = 0f;
			}
			if (Math.Abs(oldVelocity.X) > 0.3f)
				Projectile.velocity.X = oldVelocity.X * 0.75f;
			return false;
		}

		public override void OnKill(int timeLeft) {
			if (Dud) {
				// 哑弹：金光一收，"噗"地一缕烟
				WBoss.SmokeBurst(Projectile.Center, 5, 0.9f);
				SoundEngine.PlaySound(SoundID.Item66 with { Volume = 0.5f, Pitch = -0.4f }, Projectile.Center);
				return;
			}
			// 两端各自播炸烟演出
			WBoss.SmokeBurst(Projectile.Center, 44, 3.6f);
			WBoss.ShakeNearby(Projectile.Center, 8);
			SoundEngine.PlaySound(SoundID.Item66, Projectile.Center);
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.3f }, Projectile.Center);
			WBattleVisuals.Flash(0.35f, new Color(255, 230, 190));
			if (!Main.dedServ)
				Lighting.AddLight(Projectile.Center, 1.2f, 0.9f, 0.4f);
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Vector2 origin = tex.Size() * 0.5f;
			float k = Resting ? MathHelper.Clamp(Projectile.ai[0] / RestHold, 0f, 1f) : 0f;
			// 停稳后骰子发金光
			if (k > 0f) {
				Color gold = new Color(255, 200, 90, 0) * (0.35f + 0.5f * k);
				Main.EntitySpriteDraw(WTelegraph.Glow, pos, null, gold, 0f, WTelegraph.Glow.Size() * 0.5f, 0.5f + 0.7f * k, SpriteEffects.None, 0);
			}
			Main.EntitySpriteDraw(tex, pos, null, lightColor, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
			// 数字浮在骰子上方；锁定 12 后随金光放大一点
			int number = (int)Projectile.localAI[0];
			if (number > 0)
				WTelegraph.DrawDieFace(Main.spriteBatch, Projectile.Center + new Vector2(0f, -20f - 4f * k), number, 0.9f + 0.3f * k, 1f);
			return false;
		}
	}
}
