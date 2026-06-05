using System;
using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Content.Items.Armor.Supporter.Radian;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalProjectiles
{
	public class RaidianMinionGlobalProj : GlobalProjectile
	{
		public override void AI(Projectile projectile) {
			if (!projectile.minion && !projectile.sentry)
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active)
				return;

			RaidianSetPlayer radian = owner.GetModPlayer<RaidianSetPlayer>();
			if (!radian.RaidianSetActive)
				return;

			NPC marked = FindMarkedTarget(owner);
			if (marked == null)
				return;

			Vector2 toTarget = marked.Center - projectile.Center;
			if (toTarget.Length() < 24f)
				return;

			Vector2 desired = toTarget.SafeNormalize(Vector2.UnitY) * Math.Max(projectile.velocity.Length(), 4f);
			projectile.velocity = Vector2.Lerp(projectile.velocity, desired, 0.06f);
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) {
			if (damageDone <= 0 || (!projectile.minion && !projectile.sentry))
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active)
				return;

			RaidianSetPlayer radian = owner.GetModPlayer<RaidianSetPlayer>();
			if (!radian.RaidianSetActive)
				return;

			int bonus = (int)(owner.GetTotalDamage(DamageClass.Generic).ApplyTo(1f) * 0.08f);
			if (bonus > 0)
				target.SimpleStrikeNPC(bonus, 0, false, 0, DamageClass.Magic);
		}

		private static NPC FindMarkedTarget(Player owner) {
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.active || npc.friendly || npc.dontTakeDamage)
					continue;

				if (npc.GetGlobalNPC<RaidianMarkGlobalNPC>().RaidianMarked)
					return npc;
			}

			return null;
		}
	}
}
