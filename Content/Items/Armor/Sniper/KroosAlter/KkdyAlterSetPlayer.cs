using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.KroosAlter
{
	internal class KkdyAlterSetPlayer : ArknightsArmorPlayer
	{
		public bool KkdyAlterHelmetActive;
		public bool KkdyAlterSetActive;

		public override void ResetEffects() {
			KkdyAlterHelmetActive = false;
			KkdyAlterSetActive = false;
		}

		public override void PostUpdateEquips() {
			KkdyAlterHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<KkdyAlterHead>());
			KkdyAlterSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<KkdyAlterHead>(),
				ModContent.ItemType<KkdyAlterBody>(),
				ModContent.ItemType<KkdyAlterLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, KkdyAlterSetActive, "Mods.ArknightsMod.ArmorSets.KkdyAlter.SetBonus");
		}

		public override float UseSpeedMultiplier(Item item) {
			if (KkdyAlterHelmetActive && item.DamageType.CountsAsClass(DamageClass.Ranged))
				return 1.15f;

			return 1f;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (KkdyAlterSetActive && item.DamageType.CountsAsClass(DamageClass.Ranged) && Main.rand.NextFloat() < 0.2f)
				damage *= 1.5f;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryStunOnRangedProc(item, target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryStunOnRangedProc(proj, target, damageDone);
		}

		private void TryStunOnRangedProc(Item item, NPC target, int damageDone) {
			if (!KkdyAlterSetActive || damageDone <= 0 || !item.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (Main.rand.NextFloat() < 0.2f)
				OperatorStunNPC.TryApply(target, 12);
		}

		private void TryStunOnRangedProc(Projectile proj, NPC target, int damageDone) {
			if (!KkdyAlterSetActive || damageDone <= 0 || !proj.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (Main.rand.NextFloat() < 0.2f)
				OperatorStunNPC.TryApply(target, 12);
		}
	}
}
