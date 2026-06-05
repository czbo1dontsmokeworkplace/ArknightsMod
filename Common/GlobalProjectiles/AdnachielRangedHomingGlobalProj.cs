using System;
using ArknightsMod.Content.Items.Armor.Sniper.Adnachiel;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalProjectiles
{
	public class AdnachielRangedHomingGlobalProj : GlobalProjectile
	{
		public override void AI(Projectile projectile) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active || owner.dead)
				return;

			AdnachielSetPlayer adnachiel = owner.GetModPlayer<AdnachielSetPlayer>();
			if (!adnachiel.AdnachielSetActive || !projectile.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			NPC target = FindRangedCapableEnemy(owner);
			if (target == null)
				return;

			Vector2 toTarget = target.Center - projectile.Center;
			if (toTarget.Length() < 16f)
				return;

			Vector2 desired = toTarget.SafeNormalize(Vector2.UnitY) * projectile.velocity.Length();
			projectile.velocity = Vector2.Lerp(projectile.velocity, desired, 0.08f);
			projectile.netUpdate = true;
		}

		private static NPC FindRangedCapableEnemy(Player player) {
			NPC best = null;
			float bestDistance = float.MaxValue;
			float maxRange = 1200f;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
					continue;

				if (!IsRangedCapable(npc))
					continue;

				float distance = Vector2.Distance(player.Center, npc.Center);
				if (distance > maxRange || distance >= bestDistance)
					continue;

				bestDistance = distance;
				best = npc;
			}

			return best;
		}

		private static bool IsRangedCapable(NPC npc) {
			if (npc.aiStyle == NPCAIStyleID.Caster
				|| npc.aiStyle == NPCAIStyleID.Spell
				|| npc.aiStyle == NPCAIStyleID.Flying
				|| npc.aiStyle == NPCAIStyleID.SkeletronHead
				|| npc.aiStyle == NPCAIStyleID.DD2Betsy
				|| npc.aiStyle == NPCAIStyleID.DD2EterniaCrystal) {
				return true;
			}

			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (proj.active && proj.hostile && proj.owner == npc.whoAmI)
					return true;
			}

			return false;
		}
	}
}
