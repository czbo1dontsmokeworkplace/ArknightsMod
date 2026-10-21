using ArknightsMod.Content.Items.Armor.Caster.Steward;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalProjectiles
{
	public sealed class StewardArmorHomingGlobalProj : GlobalProjectile
	{
		private const float SearchRange = 600f;
		private const float TurnDegreesPerTick = 3f;

		public override void PostAI(Projectile projectile) {
			if (!ShouldHome(projectile))
				return;

			NPC target = FindHighestDefenseEnemy(projectile.Center);
			if (target == null)
				return;

			// 保留弹速，只温和调整方向；额外更新弹幕也保持每游戏帧最多转3度。
			float maxTurn = MathHelper.ToRadians(TurnDegreesPerTick) / (projectile.extraUpdates + 1);
			float angle = MathHelper.WrapAngle((target.Center - projectile.Center).ToRotation()
				- projectile.velocity.ToRotation());
			projectile.velocity = projectile.velocity.RotatedBy(MathHelper.Clamp(angle, -maxTurn, maxTurn));
		}

		private static bool ShouldHome(Projectile projectile) {
			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers
				|| !projectile.friendly || projectile.hostile || projectile.damage <= 0
				|| projectile.minion || projectile.sentry || projectile.aiStyle is 19 or 75
				|| projectile.velocity.LengthSquared() < 0.01f
				|| projectile.ModProjectile?.ShouldUpdatePosition() == false)
				return false;

			Player owner = Main.player[projectile.owner];
			if (!owner.active || owner.dead || owner.heldProj == projectile.whoAmI
				|| !owner.GetModPlayer<StewardSetPlayer>().StewardSetActive)
				return false;

			return projectile.DamageType.CountsAsClass(DamageClass.Magic)
				|| projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage;
		}

		private static NPC FindHighestDefenseEnemy(Vector2 from) {
			NPC best = null;
			float bestDistanceSq = float.MaxValue;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.friendly || npc.dontTakeDamage || npc.immortal || npc.lifeMax <= 5 || !npc.CanBeChasedBy())
					continue;
				float distanceSq = Vector2.DistanceSquared(from, npc.Center);
				if (distanceSq > SearchRange * SearchRange
					|| !Collision.CanHitLine(from, 1, 1, npc.Center, 1, 1))
					continue;

				if (best == null || npc.defense > best.defense
					|| (npc.defense == best.defense && distanceSq < bestDistanceSq)) {
					best = npc;
					bestDistanceSq = distanceSq;
				}
			}
			return best;
		}
	}
}
