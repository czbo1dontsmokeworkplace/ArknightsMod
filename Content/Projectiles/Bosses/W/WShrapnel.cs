using ArknightsMod.Content.NPCs.Enemy.W;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// 阔剑雷的定向碎片：触发后从雷的正面锥形喷出的一扇高速破片，短寿命、略受重力、碰砖即灭。<br/>
	/// 命中附带混乱 5s（沿用阔剑雷的效果）。无贴图，画成沿速度方向的一条亮线。
	/// </summary>
	public class WShrapnel : ModProjectile
	{
		public override string Texture => "Terraria/Images/MagicPixel";

		private const int LifeUpdates = 34; // 按 AI 调用次数计寿命（extraUpdates 下 timeLeft 的语义不稳）

		public override void SetDefaults() {
			Projectile.width = 6;
			Projectile.height = 6;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 120;
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			if (++Projectile.localAI[0] >= LifeUpdates) {
				Projectile.Kill();
				return;
			}
			Projectile.velocity.Y += 0.06f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (!Main.dedServ) {
				Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.1f);
				if (Main.rand.NextBool(3)) {
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.15f, 0, default, 0.9f);
					d.noGravity = true;
				}
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) {
			target.AddBuff(BuffID.Confused, 300);
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (!Main.dedServ) {
				for (int i = 0; i < 3; i++) {
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -oldVelocity.RotatedByRandom(0.6f) * 0.2f, 0, default, 1.1f);
					d.noGravity = true;
				}
			}
			return true;
		}

		public override bool PreDraw(ref Color lightColor) {
			float speed = Projectile.velocity.Length();
			float len = MathHelper.Clamp(speed * 1.6f, 10f, 26f);
			float fade = MathHelper.Clamp((LifeUpdates - Projectile.localAI[0]) / 10f, 0f, 1f);
			WTelegraph.BeginAdditive(Main.spriteBatch);
			WTelegraph.DrawLine(Main.spriteBatch, Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * len, Projectile.rotation, len, 2.5f, new Color(255, 150, 90) * fade);
			WTelegraph.DrawLine(Main.spriteBatch, Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * len * 0.6f, Projectile.rotation, len * 0.6f, 1.2f, new Color(255, 240, 200) * fade);
			WTelegraph.EndAdditive(Main.spriteBatch);
			return false;
		}
	}
}
