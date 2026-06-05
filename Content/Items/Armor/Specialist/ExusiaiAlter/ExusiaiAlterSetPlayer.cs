using System;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Projectiles.Specialist.ExusiaiAlter;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter
{
	internal class ExusiaiAlterSetPlayer : ArknightsArmorPlayer
	{
		public bool ExusiaiAlterHelmetActive;
		public bool ExusiaiAlterSetActive;

		public override void ResetEffects() {
			ExusiaiAlterHelmetActive = false;
			ExusiaiAlterSetActive = false;
		}

		public override void PostUpdateEquips() {
			ExusiaiAlterHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<ExusiaiAlterHelmet>());
			ExusiaiAlterSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<ExusiaiAlterHelmet>(),
				ModContent.ItemType<ExusiaiAlterChestplate>(),
				ModContent.ItemType<ExusiaiAlterGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, ExusiaiAlterSetActive, "Mods.ArknightsMod.ArmorSets.ExusiaiAlter.SetBonus");
		}

		public static bool IsExusiaiAlterHelmetOnField() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (player.active && !player.dead && player.GetModPlayer<ExusiaiAlterSetPlayer>().ExusiaiAlterHelmetActive)
					return true;
			}

			return false;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (!IsExusiaiAlterHelmetOnField() || !OperatorAmmoConsumeHelper.HasAmmoSkill(Player))
				return;

			float mult = OperatorTagHelper.PlayerHasFaction(Player, OperatorFaction.Laterano) ? 1.18f : 1.09f;
			damage *= mult;
		}

		private int lifeDrainTimer;

		public override void PostUpdate() {
			if (!ExusiaiAlterSetActive || Player.dead || Main.netMode == NetmodeID.MultiplayerClient)
				return;

			lifeDrainTimer++;
			if (lifeDrainTimer >= 30 && Player.statLife > 0) {
				lifeDrainTimer = 0;
				Player.statLife--;
			}
		}

		public void OnAllyAmmoConsumed(Player ally) {
			if (!ExusiaiAlterSetActive || ally.whoAmI == Player.whoAmI || Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int heal = Math.Max(1, (int)(Player.statLifeMax2 * 0.06f));
			Player.Heal(heal);
			Player.HealEffect(heal);

			if (Main.rand.NextFloat() >= 0.25f)
				return;

			NPC target = FindNearestEnemyToPlayer(ally, 600f);
			if (target == null)
				return;

			int baseAttack = Player.HeldItem?.damage ?? 10;
			int damage = (int)(Player.GetTotalDamage(DamageClass.Ranged).ApplyTo(baseAttack) * 1.5f);
			Projectile.NewProjectile(
				Player.GetSource_FromThis(),
				ally.Center,
				Vector2.Zero,
				ModContent.ProjectileType<ExusiaiAlterBomb>(),
				Math.Max(1, damage),
				4f,
				Player.whoAmI,
				target.whoAmI);
		}

		private static NPC FindNearestEnemyToPlayer(Player ally, float range) {
			NPC best = null;
			float bestDist = range;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.active || !npc.CanBeChasedBy() || npc.friendly)
					continue;

				float dist = Vector2.Distance(ally.Center, npc.Center);
				if (dist < bestDist) {
					bestDist = dist;
					best = npc;
				}
			}

			return best;
		}
	}
}
