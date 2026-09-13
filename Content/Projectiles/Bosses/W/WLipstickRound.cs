using ArknightsMod.Content.NPCs.Enemy.W;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// 口红弹（榴弹发射器弹药），一类三模式：<br/>
	/// ai[0]=0 红桃K 直线快弹——爆炸范围扩至雷管级，命中 200% + 混乱 5s；ai[1]&lt;0 时为定时引信（-tick），到点在空中自爆（满注用）；<br/>
	/// ai[0]=1 二阶段普攻抛物线弹——短暂平飞后下坠，触玩家/触块/超射程爆炸，手雷范围 100%；<br/>
	/// ai[0]=2 曲射弹——出膛即受重力，高抛约 1.7s 后落地，手雷范围 100%，落点由 W 求解并写进 ai[1]/ai[2]（地面 X 标记用）；<br/>
	/// ai[0]=3 地毯弹——直线快弹 + 定时引信（ai[1]&lt;0），80px 桃红爆，混乱 2s；五发沿地面连爆成一道推进的墙。
	/// </summary>
	public class WLipstickRound : ModProjectile
	{
		private const float KingBlastRadius = 125f; // 雷管范围参照（原版雷管爆炸盒 250x250）
		private const float ArcBlastRadius = 64f;   // 手雷范围
		private const float CarpetBlastRadius = 80f;

		private bool IsKingRound => Projectile.ai[0] == 0f;
		private bool IsMortar => Projectile.ai[0] == 2f;
		private bool IsCarpet => Projectile.ai[0] == 3f;
		/// <summary>直线弹（红桃K / 地毯）：不吃重力、可带定时引信</summary>
		private bool IsStraight => IsKingRound || IsCarpet;

		public override void SetDefaults() {
			Projectile.width = 12;
			Projectile.height = 8;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 300;
			Projectile.aiStyle = -1;
		}

		public override bool CanHitPlayer(Player target) => false;

		public override void AI() {
			if (IsMortar) {
				// 曲射：全程重力，寿命够飞完整条高抛线
				Projectile.velocity.Y += 0.25f;
				if (Projectile.velocity.Y > 16f)
					Projectile.velocity.Y = 16f;
			}
			else if (!IsStraight) {
				// 二阶段普攻：短射程 + 抛物线下坠
				if (Projectile.timeLeft > 150)
					Projectile.timeLeft = 150;
				if (Projectile.ai[1]++ > 10) {
					Projectile.velocity.Y += 0.2f;
					if (Projectile.velocity.Y > 15f)
						Projectile.velocity.Y = 15f;
				}
			}
			else if (Projectile.ai[1] < 0f) {
				// 定时引信：飞到预定落点就在空中炸，别射进天里
				Projectile.ai[1]++;
				if (Projectile.ai[1] >= 0f && Main.netMode != NetmodeID.MultiplayerClient) {
					Projectile.Kill();
					return;
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (!Main.dedServ) {
				// 红桃K/地毯弹更亮更密：它们是重的那几发，飞行中就该看得出分量
				int trailChance = IsStraight ? 1 : 2;
				if (Main.rand.NextBool(trailChance)) {
					// 红桃K 系的轨迹偏桃红，一眼分得出这是那发带牌的
					Color trail = IsStraight ? new Color(255, 70, 110) : Color.Red;
					Dust d = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f, DustID.GemRuby, -Projectile.velocity * 0.1f, 100, trail, IsStraight ? 1.1f : 0.9f);
					d.noGravity = true;
				}
				if (IsStraight)
					Lighting.AddLight(Projectile.Center, 0.8f, 0.15f, 0.1f);
				else if (IsMortar && Projectile.velocity.Y > 0f && Main.rand.NextBool(2)) {
					// 下坠段拖一缕烟，让"从天上掉下来"读得出来
					Dust s = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, -Projectile.velocity * 0.05f, 140, default, 0.8f);
					s.noGravity = true;
				}
			}

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				for (int i = 0; i < Main.maxPlayers; i++) {
					Player player = Main.player[i];
					if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox)) {
						Projectile.Kill();
						return;
					}
				}
			}
		}

		public override void OnKill(int timeLeft) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				float radius = IsKingRound ? KingBlastRadius : (IsCarpet ? CarpetBlastRadius : ArcBlastRadius);
				float confused = IsKingRound ? 300f : (IsCarpet ? 120f : 0f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<WExplosion>(), Projectile.damage, IsStraight ? 8f : 5f, Main.myPlayer,
					ai0: radius, ai1: confused, ai2: IsStraight ? 1f : 0f);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			// 曲射弹：落点 X 标记——W 出膛时把预定落点塞进 ai[1]/ai[2]，下坠段越近越亮
			if (IsMortar && (Projectile.ai[1] != 0f || Projectile.ai[2] != 0f)) {
				float k = Projectile.velocity.Y > 0f ? MathHelper.Clamp(0.35f + Projectile.velocity.Y / 14f, 0.35f, 1f) : 0.25f;
				WTelegraph.BeginAdditive(Main.spriteBatch);
				WTelegraph.DrawGroundX(Main.spriteBatch, new Vector2(Projectile.ai[1], Projectile.ai[2]), 26f, k);
				WTelegraph.EndAdditive(Main.spriteBatch);
			}
			return false;
		}
	}
}
