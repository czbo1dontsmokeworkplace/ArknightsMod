using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Bosses.W
{
	/// <summary>
	/// W 的可远程起爆弹药（阔剑雷、跳雷、D12 等）。<br/>
	/// WBoss 通过扫描场上实现了本接口的弹幕来做「倒数计时→全场起爆」「烟带撒雷→顺序起爆」与死亡清场。
	/// </summary>
	public interface IWOrdnance
	{
		/// <summary>已布设/已黏附/已点燃——可被远程起爆（飞行中的不算）</summary>
		bool IsLive { get; }

		/// <summary>远程起爆：ticks 帧后爆炸；若已在更短的倒数中则保留原值（仅服务端/单机端调用，内部自行 netUpdate）</summary>
		void Prime(int ticks);
	}

	/// <summary>布设类弹药共用的物理小工具</summary>
	public static class WOrdnanceUtil
	{
		/// <summary>
		/// 带平台支撑的下落（tileCollide 关掉、手动 TileCollision，fallThrough=false 时会停在平台顶面）。<br/>
		/// 重力 0.3/tick，与 WBoss.SolveLob 抛投时用的重力一致。返回 true 表示本帧踩到了地面。
		/// </summary>
		public static bool ApplyGroundedFall(Projectile projectile) {
			projectile.velocity.Y += 0.3f;
			if (projectile.velocity.Y > 14f)
				projectile.velocity.Y = 14f;
			float fallIntent = projectile.velocity.Y;
			Vector2 adjusted = Collision.TileCollision(projectile.position, projectile.velocity,
				projectile.width, projectile.height, fallThrough: false, fall2: false, gravDir: 1);
			bool grounded = fallIntent > 0.1f && adjusted.Y < fallIntent - 0.05f;
			projectile.velocity = adjusted;
			if (grounded)
				projectile.velocity.Y = 0f;
			return grounded;
		}
	}
}
