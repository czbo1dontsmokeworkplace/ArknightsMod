using System;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi
{
	internal class SkadiSetPlayer : ArknightsArmorPlayer
	{
		public bool SkadiHelmetActive;
		public bool SkadiSetActive;

		public bool UndyingTriggered;
		public bool UndyingBuffActive;
		private int lifeMaxPenalty;

		public override void ResetEffects() {
			SkadiHelmetActive = false;
			SkadiSetActive = false;
		}

		public override void PostUpdateEquips() {
			SkadiHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<SkadiHead>());
			SkadiSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<SkadiHead>(),
				ModContent.ItemType<SkadiBody>(),
				ModContent.ItemType<SkadiLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, SkadiSetActive, "Mods.ArknightsMod.ArmorSets.Skadi.SetBonus");
		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;

			if (lifeMaxPenalty > 0)
				health.Base -= lifeMaxPenalty;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (SkadiHelmetActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				damage *= 1.2f;
		}

		public override float UseSpeedMultiplier(Item item) {
			if (UndyingBuffActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				return 1.3f;

			return 1f;
		}

		public override void UpdateDead() {
			if (SkadiSetActive)
				Player.respawnTimer = Math.Max(0, Player.respawnTimer - 5 * 60);
		}

		public override bool FreeDodge(Player.HurtInfo info) {
			if (!SkadiSetActive || UndyingTriggered || !OperatorSetBossHelper.AnyBossActive())
				return false;

			if (Player.statLife - info.Damage > 0)
				return false;

			TriggerUndying();
			return true;
		}

		public override void PostUpdate() {
			if (!OperatorSetBossHelper.AnyBossActive())
				UndyingBuffActive = false;
		}

		public override void OnRespawn() {
			ResetDeploymentState();
		}

		private void TriggerUndying() {
			UndyingTriggered = true;
			UndyingBuffActive = true;

			int currentMax = Player.statLifeMax2;
			lifeMaxPenalty += (int)(currentMax * 0.7f);
			Player.statLife = currentMax - lifeMaxPenalty;
			Player.HealEffect(Player.statLife);
		}

		private void ResetDeploymentState() {
			UndyingTriggered = false;
			UndyingBuffActive = false;
			lifeMaxPenalty = 0;
		}
	}
}
