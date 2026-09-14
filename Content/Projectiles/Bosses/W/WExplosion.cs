using ArknightsMod.Common.Particle;
using ArknightsMod.Content.NPCs.Enemy.W;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// W 系炸弹共用的爆炸判定体（由服务端在引爆点生成，保证联机两端一致）。<br/>
	/// ai[0] = 爆炸半径(px)；ai[1] = 命中后混乱时长(tick，0 = 不上混乱，&lt;0 = 纯演出不伤人)；<br/>
	/// ai[2] = 样式 + 10×寿命：样式 0 常规橙红 / 1 红桃K 桃红 / 2 JACKPOT 金 / 3 核（白炽→橙）；寿命 0 取默认 26 tick，屏幕级的大波给 40~60。<br/>
	/// 伤害窗口仅前 6 tick；其余生命周期用于冲击波着色器演出（WShockwave.fx，加载失败退化为纯粒子）。
	/// 半径 ≥110 的大爆多一圈延迟冲击波、更多火星、更长震屏，并给附近玩家一下随距离衰减的白闪。
	/// </summary>
	public class WExplosion : ModProjectile
	{
		private const int DefaultLifetime = 26;
		private const int DamageWindow = 6;

		public override string Texture => "Terraria/Images/MagicPixel";

		private float Age => Projectile.localAI[0];
		private float Radius => Projectile.ai[0] < 16f ? 16f : Projectile.ai[0];
		private int Style => (int)Projectile.ai[2] % 10;
		private int Lifetime => (int)Projectile.ai[2] / 10 > 0 ? (int)Projectile.ai[2] / 10 : DefaultLifetime;
		private bool Big => Radius >= 110f;
		private bool Cosmetic => Projectile.ai[1] < 0f;
		private bool Nuke => Style == 3;

		private Color WaveColor => Style switch {
			1 => new Color(255, 80, 120),
			2 => new Color(255, 205, 90),
			3 => new Color(255, 190, 140),
			_ => new Color(255, 107, 46),
		};

		private Color SparkColor => Style switch {
			1 => new Color(255, 120, 150),
			2 => new Color(255, 225, 140),
			3 => new Color(255, 200, 150),
			_ => new Color(255, 150, 70),
		};

		public override void SetDefaults() {
			Projectile.width = 32;
			Projectile.height = 32;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 120; // 实际寿命由 Lifetime 控制，见 AI
			Projectile.alpha = 255;
			Projectile.aiStyle = -1;
		}

		// 伤害只在起爆窗口内结算，后续帧纯演出；纯演出体永不伤人
		public override bool CanHitPlayer(Player target) => !Cosmetic && Age <= DamageWindow;

		public override void AI() {
			if (Projectile.localAI[0] == 0f) {
				int radius = (int)Radius;
				Projectile.Resize(radius * 2, radius * 2);
				Projectile.timeLeft = Lifetime;

				// 爆点演出（两端各自播放）；核爆的声音、震屏、白闪由骰子的 OnKill 统一给，这里不叠
				if (!Nuke) {
					SoundEngine.PlaySound(SoundID.Item14 with { Pitch = Big ? -0.15f : 0.1f }, Projectile.Center);
					if (Style == 2)
						SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.2f, Volume = 0.7f }, Projectile.Center);
					WBoss.ShakeNearby(Projectile.Center, Big ? 9 : 5);
				}
				if (Big && !Main.dedServ && !Nuke) {
					// 大爆：离得越近白闪越亮（700px 外不闪）
					float dist = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
					float k = MathHelper.Clamp(1f - dist / 700f, 0f, 1f);
					if (k > 0f)
						WBattleVisuals.Flash(0.08f + 0.14f * k, Style == 2 ? new Color(255, 235, 190) : new Color(255, 225, 210));
				}
				if (!Main.dedServ) {
					// 每次爆炸都照一下天空的云底：半径越大越亮（400px 及以上拉满）
					WBattleVisuals.SkyFlash(Projectile.Center, MathHelper.Clamp(Radius / 400f, 0.15f, 1f));
					Color spark = SparkColor;
					int dustCount = radius / 2;
					for (int i = 0; i < dustCount; i++) {
						Vector2 vel = Main.rand.NextVector2Circular(1f, 1f) * (radius / 18f);
						Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius * 0.4f, radius * 0.4f), DustID.Torch, vel, 0, default, Main.rand.NextFloat(1.2f, 2.2f));
						fire.noGravity = true;
						if (Style != 0)
							fire.color = spark;
					}
					for (int i = 0; i < dustCount / 2; i++) {
						Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius * 0.5f, radius * 0.5f), DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), 110, default, Main.rand.NextFloat(1.2f, 1.8f));
						smoke.noGravity = Main.rand.NextBool();
					}
					// 放射状火星：数量与飞速随半径涨，拉长成线条，快的先死慢的后死；大爆再多一半
					int sparks = Math.Max(6, radius / 8) * (Big ? 3 : 2) / 2;
					for (int i = 0; i < sparks; i++) {
						Vector2 dir = Main.rand.NextVector2Unit();
						float speed = Main.rand.NextFloat(4f, 7f) * (0.6f + radius / 125f);
						var p = new DefaultParticle(Projectile.Center + dir * 6f, dir * speed, (int)(34 - speed), Main.rand.NextFloat(1.0f, 1.7f), spark, false) {
							Deformation = new Vector2(0.1f, 0.85f) * Main.rand.NextFloat(0.8f, 1.4f),
						};
						p.Spawn();
					}
					// 起爆瞬间的白闪核
					var core = new DefaultParticle(Projectile.Center, Vector2.Zero, 9, radius / 26f, Style == 2 ? new Color(255, 240, 200) : new Color(255, 235, 200), true) {
						Deformation = Vector2.One,
					};
					core.Spawn();
				}
			}
			if (!Main.dedServ && Age < 8f) {
				Color w = WaveColor;
				float k = 1f - Age / 8f;
				Lighting.AddLight(Projectile.Center, w.R / 255f * 1.3f * k, w.G / 255f * 1.3f * k, w.B / 255f * 1.3f * k);
			}
			Projectile.localAI[0]++;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) {
			int confusedTicks = (int)Projectile.ai[1];
			if (confusedTicks > 0)
				target.AddBuff(BuffID.Confused, confusedTicks);
		}

		public override bool? CanDamage() => Cosmetic ? false : null;

		public override bool PreDraw(ref Color lightColor) {
			// 冲击波着色器演出（镜像 LavaExplosionShaderEffect 的实体着色器用法）
			if (ArknightsMod.WShockwaveEffect == null)
				return false;

			float progress = MathHelper.Clamp(Age / Lifetime, 0f, 1f);
			Texture2D tex = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Bosses/W/WSmoke").Value;
			float scale = Radius * 2.4f / tex.Width;
			Color wave = WaveColor;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			var fx = ArknightsMod.WShockwaveEffect.Value;
			fx.Parameters["opacity"].SetValue(1f);
			fx.Parameters["waveColor"].SetValue(new Vector4(wave.R / 255f, wave.G / 255f, wave.B / 255f, 1f));
			// 主波两遍叠亮；大爆再跟一圈延迟 22% 的次波
			int passes = Big ? 2 : 1;
			for (int ring = 0; ring < passes; ring++) {
				float p = progress - ring * 0.22f;
				if (p < 0f)
					continue;
				fx.Parameters["progress"].SetValue(p);
				fx.CurrentTechnique.Passes[0].Apply();
				int layers = ring == 0 ? 2 : 1;
				for (int i = 0; i < layers; i++) {
					Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null,
						Color.White, 0f, tex.Size() * 0.5f, scale * (ring == 0 ? 1f : 0.8f), SpriteEffects.None, 0);
				}
			}

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			return false;
		}
	}
}
