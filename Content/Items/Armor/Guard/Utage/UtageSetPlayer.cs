using System;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Utage
{
	internal class UtageSetPlayer : ArknightsArmorPlayer
	{
		public bool UtageHelmetActive;
		public bool UtageSetActive;

		private int helmetHealCount;
		private int helmetHealTimer;

		public override void ResetEffects() {
			UtageHelmetActive = false;
			UtageSetActive = false;
		}

		public override void PostUpdateEquips() {
			UtageHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<UtageHelmet>());
			UtageSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<UtageHelmet>(),
				ModContent.ItemType<UtageChestplate>(),
				ModContent.ItemType<UtageGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(
				Player,
				UtageSetActive,
				"Mods.ArknightsMod.ArmorSets.Utage.SetBonus");
		}

		public override void PostUpdate() {
			if (helmetHealTimer > 0) {
				helmetHealTimer--;
				if (helmetHealTimer <= 0)
					helmetHealCount = 0;
			}
		}

		public float GetMeleeAttackSpeedBonusPercent() {
			if (!UtageSetActive)
				return 0f;

			float missingRatio = 1f - Player.statLife / (float)Player.statLifeMax2;
			return Math.Min(missingRatio * 0.5f, 0.3f) * 100f;
		}

		public override float UseSpeedMultiplier(Item item) {
			if (!UtageSetActive || !item.DamageType.CountsAsClass(DamageClass.Melee))
				return 1f;

			return 1f + GetMeleeAttackSpeedBonusPercent() / 100f;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			if (!UtageHelmetActive || damageDone <= 0)
				return;

			if (!item.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			TryUtageHelmetHeal(target);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			if (!UtageHelmetActive || damageDone <= 0)
				return;

			if (!proj.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			TryUtageHelmetHeal(target);
		}

		private void TryUtageHelmetHeal(NPC target) {
			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage || target.immortal)
				return;

			if (helmetHealTimer <= 0) {
				helmetHealTimer = 60;
				helmetHealCount = 0;
			}

			if (helmetHealCount >= 3)
				return;

			if (Player.statLife >= Player.statLifeMax2)
				return;

			Player.Heal(1);
			helmetHealCount++;
		}
	}
}
