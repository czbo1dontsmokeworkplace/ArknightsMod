using ArknightsMod.Common.Particle;
using ArknightsMod.Content.NPCs.Enemy.W;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// 蘑菇云（纯演出）：烟柱从爆点升起（40 tick），云盖在顶端向两侧摊开，核心由白炽→橙→暗灰，最后整体淡去。<br/>
	/// 全程程序化：软烟贴图分层堆叠 + 沿柱身上涌的烟尘 + 升腾火星。不伤人。
	/// </summary>
	public class WNukeCloud : ModProjectile
	{
		public override string Texture => "Terraria/Images/MagicPixel";

		private const int Lifetime = 210;
		private const float StemHeight = 230f;
		private const float CapRadius = 150f;

		private float Age => Lifetime - Projectile.timeLeft;
		private float Rise => MathHelper.Clamp(Age / 40f, 0f, 1f);                 // 烟柱升起
		private float Spread => MathHelper.Clamp((Age - 24f) / 50f, 0f, 1f);       // 云盖摊开
		private float Fade => MathHelper.Clamp((Lifetime - Age) / 70f, 0f, 1f);    // 尾段淡出
		private float Heat => MathHelper.Clamp(1f - Age / 90f, 0f, 1f);            // 白炽→橙→灰

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.velocity = Vector2.Zero;
			if (Main.dedServ)
				return;
			Vector2 baseP = Projectile.Center;
			float heat = Heat;
			Lighting.AddLight(baseP + new Vector2(0f, -StemHeight * Rise * 0.6f), 1.4f * heat + 0.2f, 0.7f * heat + 0.1f, 0.3f * heat);

			// 沿柱身上涌的烟尘
			int puffs = Age < 60f ? 4 : 2;
			for (int i = 0; i < puffs; i++) {
				float h = Main.rand.NextFloat() * StemHeight * Rise;
				Vector2 pos = baseP + new Vector2(Main.rand.NextFloat(-40f, 40f) * (0.5f + h / StemHeight), -h);
				Dust d = Dust.NewDustPerfect(pos, DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(1.5f, 3.5f)), 80, default, Main.rand.NextFloat(1.6f, 2.4f));
				d.color = Color.Lerp(new Color(255, 150, 80), new Color(70, 55, 55), MathHelper.Clamp(h / StemHeight + (1f - heat), 0f, 1f));
				d.noGravity = true;
			}
			// 升腾火星
			if (Age < 80f && Main.rand.NextBool(2)) {
				var p = new DefaultParticle(baseP + new Vector2(Main.rand.NextFloat(-30f, 30f), -Main.rand.NextFloat(0f, 60f)),
					new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -Main.rand.NextFloat(3f, 7f)), Main.rand.Next(30, 50), Main.rand.NextFloat(1.0f, 1.6f), new Color(255, 170, 90), true) {
					Deformation = new Vector2(0.12f, 0.8f),
				};
				p.Spawn();
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D smoke = WTelegraph.Glow;
			Vector2 baseP = Projectile.Center;
			float rise = Rise, spread = Spread, fade = Fade, heat = Heat;
			Vector2 origin = smoke.Size() * 0.5f;
			Color hot = new(255, 200, 150);
			Color warm = new(200, 90, 50);
			Color ash = new(70, 58, 60);

			// 烟柱：从下往上 8 团，越高越淡越大；核心颜色随 heat 从白炽到暗灰
			for (int i = 0; i < 8; i++) {
				float t = (i + 0.5f) / 8f;
				if (t > rise)
					break;
				float wobble = MathF.Sin(Main.GlobalTimeWrappedHourly * 2.2f + i * 1.3f) * 6f;
				Vector2 pos = baseP + new Vector2(wobble, -StemHeight * t);
				Color c = Color.Lerp(Color.Lerp(ash, warm, heat * (1f - t * 0.6f)), hot, heat * heat * (1f - t)) * (0.85f * fade);
				float scale = (1.1f + t * 0.5f) * (0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + i));
				Main.EntitySpriteDraw(smoke, pos - Main.screenPosition, null, c, i * 0.7f + Main.GlobalTimeWrappedHourly * 0.3f, origin, scale, SpriteEffects.None, 0);
			}

			// 云盖：顶端一圈 10 团向外摊开，中间再压一团最亮的核
			if (spread > 0f) {
				Vector2 top = baseP + new Vector2(0f, -StemHeight * rise);
				for (int i = 0; i < 10; i++) {
					float a = MathHelper.TwoPi * i / 10f + Main.GlobalTimeWrappedHourly * 0.25f;
					Vector2 pos = top + a.ToRotationVector2() * new Vector2(CapRadius * spread, CapRadius * 0.45f * spread);
					Color c = Color.Lerp(ash, warm, heat * 0.7f) * (0.8f * fade);
					float scale = (1.5f + 0.4f * spread) * (0.92f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f + i));
					Main.EntitySpriteDraw(smoke, pos - Main.screenPosition, null, c, a, origin, scale, SpriteEffects.None, 0);
				}
				Color core = Color.Lerp(warm, hot, heat) * (0.9f * fade);
				Main.EntitySpriteDraw(smoke, top - Main.screenPosition, null, core, Main.GlobalTimeWrappedHourly * 0.5f, origin, 2.2f + 0.6f * spread, SpriteEffects.None, 0);
			}

			// 地面一层横向摊开的尘浪
			float ground = MathHelper.Clamp(Age / 30f, 0f, 1f);
			for (int i = -3; i <= 3; i++) {
				Vector2 pos = baseP + new Vector2(i * 60f * ground, -10f + Math.Abs(i) * 4f);
				Color c = ash * (0.55f * fade * (1f - Math.Abs(i) / 4f));
				Main.EntitySpriteDraw(smoke, pos - Main.screenPosition, null, c, i * 0.4f, origin, 1.4f * ground, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
