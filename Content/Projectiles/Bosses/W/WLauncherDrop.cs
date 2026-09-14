using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// W 倒下时脱手的榴弹发射器（纯演出）：翻着跟头落地，弹两下，躺平，几秒后淡去。<br/>
	/// 用的就是手持叠绘那张发射器贴图。
	/// </summary>
	public class WLauncherDrop : ModProjectile
	{
		public override string Texture => "ArknightsMod/Content/NPCs/Enemy/W/WBossLauncher";

		private const int Lifetime = 330;
		private const int FadeTicks = 60;

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 14;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			bool resting = Projectile.ai[0] == 1f;
			Projectile.velocity.Y += 0.3f;
			if (Projectile.velocity.Y > 12f)
				Projectile.velocity.Y = 12f;
			if (!resting) {
				Projectile.velocity.X *= 0.99f;
				Projectile.rotation += Projectile.velocity.X * 0.06f + 0.11f * Projectile.direction;
			}
			else {
				Projectile.velocity.X = 0f;
				// 躺平：转角收敛到最近的水平位
				float flat = MathF.Round(Projectile.rotation / MathHelper.Pi) * MathHelper.Pi;
				Projectile.rotation = MathHelper.Lerp(Projectile.rotation, flat, 0.25f);
			}
			if (Projectile.timeLeft < FadeTicks)
				Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Projectile.timeLeft / (float)FadeTicks);
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (Math.Abs(oldVelocity.Y) > 1.8f) {
				// 金属落地的一声脆响，弹得越低声音越轻
				Projectile.velocity.Y = oldVelocity.Y * -0.4f;
				Projectile.velocity.X = oldVelocity.X * 0.6f;
				SoundEngine.PlaySound(SoundID.Tink with { Volume = MathHelper.Clamp(Math.Abs(oldVelocity.Y) / 8f, 0.3f, 1f), Pitch = -0.2f }, Projectile.Center);
			}
			else {
				Projectile.velocity.Y = 0f;
				Projectile.velocity.X = 0f;
				Projectile.ai[0] = 1f;
			}
			return false;
		}
	}
}
