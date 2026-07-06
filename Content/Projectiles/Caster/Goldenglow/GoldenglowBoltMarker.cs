using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow
{
	// 标记某个原版魔法导弹弹幕是否由澄闪发射，用于统计弹幕堆叠数量上限
	public class GoldenglowBoltMarker : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		public bool IsGoldenglowBolt;

		public override void OnKill(Projectile projectile, int timeLeft) {
			if (IsGoldenglowBolt) {
				Main.player[projectile.owner].GetModPlayer<GoldenglowBeaconPlayer>().BoltCount--;
			}
		}
	}
}
