using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.W
{
	/// <summary>
	/// 飘落的 ♥K 卡牌（纯演出粒子）：左右摆着往下落，一路慢慢翻面，最后淡出。<br/>
	/// 贴图字段只为满足粒子系统的加载约定，实际绘制走 <see cref="WTelegraph.DrawCard"/>。
	/// </summary>
	public class WCardParticle : Particle
	{
		public override string TexturePath => "ArknightsMod/Content/Projectiles/Bosses/W/WD12";

		private float flip;
		private float sway;

		public WCardParticle(Vector2 position, Vector2 velocity, float startFlip = 0f) {
			Position = position;
			Velocity = velocity;
			Lifetime = 160;
			Scale = 1.4f;
			Color = Microsoft.Xna.Framework.Color.White;
			flip = startFlip;
			sway = Main.rand.NextFloat(MathHelper.TwoPi);
		}

		public override void Update() {
			Velocity.X *= 0.97f;
			Velocity.Y = Math.Min(Velocity.Y + 0.05f, 1.0f);
			sway += 0.085f;
			Position.X += MathF.Sin(sway) * 0.9f;
			Rotation = MathF.Sin(sway) * 0.45f;
			flip += 0.018f;
			float r = LifetimeRatio;
			Opacity = r > 0.8f ? 1f - (r - 0.8f) / 0.2f : 1f;
		}

		public override void Draw() {
			WTelegraph.DrawCard(Main.spriteBatch, Position, Rotation, flip, Scale, Opacity);
		}
	}
}
