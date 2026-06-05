using System;
using ArknightsMod.Content.Items.Armor.Caster.Steward;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalProjectiles
{
	public class StewardMagicHomingGlobalProj : GlobalProjectile
	{
		public override void AI(Projectile projectile) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active || owner.dead)
				return;

			StewardSetPlayer steward = owner.GetModPlayer<StewardSetPlayer>();
			if (!steward.StewardSetActive || !projectile.DamageType.CountsAsClass(DamageClass.Magic))
				return;

			NPC target = FindHighestDefenseEnemy(owner);
			if (target == null)
				return;

			Vector2 toTarget = target.Center - projectile.Center;
			if (toTarget.Length() < 16f)
				return;

			Vector2 desired = toTarget.SafeNormalize(Vector2.UnitY) * projectile.velocity.Length();
			projectile.velocity = Vector2.Lerp(projectile.velocity, desired, 0.08f);
			projectile.netUpdate = true;
		}

		private static NPC FindHighestDefenseEnemy(Player player) {
			NPC best = null;
			int bestDefense = int.MinValue;
			float maxRange = 1200f;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
					continue;

				if (Vector2.Distance(player.Center, npc.Center) > maxRange)
					continue;

				if (npc.defense > bestDefense) {
					bestDefense = npc.defense;
					best = npc;
				}
			}

			return best;
		}
	}
}
