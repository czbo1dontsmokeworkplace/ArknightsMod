using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.LaPluma
{
	internal class LaPlumaSetPlayer : ArknightsArmorPlayer
	{
		public bool LaPlumaHelmetActive;
		public bool LaPlumaSetActive;

		public int KillSpeedStacks;

		public override void ResetEffects() {
			LaPlumaHelmetActive = false;
			LaPlumaSetActive = false;
		}

		public override void PostUpdateEquips() {
			LaPlumaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<LaPlumaHead>());
			LaPlumaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<LaPlumaHead>(),
				ModContent.ItemType<LaPlumaBody>(),
				ModContent.ItemType<LaPlumaLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, LaPlumaSetActive, "Mods.ArknightsMod.ArmorSets.LaPluma.SetBonus");
		}

		public override float UseSpeedMultiplier(Item item) {
			if (LaPlumaSetActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				return 1f + 0.03f * KillSpeedStacks;

			return 1f;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryHelmetHeal(item, target, damageDone);
			TryAddKillStack(item, target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryHelmetHeal(proj, target, damageDone);
			TryAddKillStack(proj, target, damageDone);
		}

		private void TryHelmetHeal(Item item, NPC target, int damageDone) {
			if (!LaPlumaHelmetActive || damageDone <= 0)
				return;

			if (!item.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage || target.immortal)
				return;

			if (Player.statLife >= Player.statLifeMax2)
				return;

			Player.Heal(2);
		}

		private void TryHelmetHeal(Projectile proj, NPC target, int damageDone) {
			if (!LaPlumaHelmetActive || damageDone <= 0)
				return;

			if (!proj.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage || target.immortal)
				return;

			if (Player.statLife >= Player.statLifeMax2)
				return;

			Player.Heal(2);
		}

		private void TryAddKillStack(Item item, NPC target, int damageDone) {
			if (!LaPlumaSetActive || damageDone <= 0)
				return;

			if (!item.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			if (target.life > 0 || target.lifeMax <= 5)
				return;

			KillSpeedStacks = System.Math.Min(12, KillSpeedStacks + 1);
		}

		private void TryAddKillStack(Projectile proj, NPC target, int damageDone) {
			if (!LaPlumaSetActive || damageDone <= 0)
				return;

			if (!proj.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			if (target.life > 0 || target.lifeMax <= 5)
				return;

			KillSpeedStacks = System.Math.Min(12, KillSpeedStacks + 1);
		}

		public override void UpdateDead() {
			KillSpeedStacks = 0;
		}
	}
}
