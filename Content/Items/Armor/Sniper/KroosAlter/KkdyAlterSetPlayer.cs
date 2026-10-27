using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
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

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 KkdyAlterHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			KkdyAlterHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<KkdyAlterHead>();
			KkdyAlterSetActive = KkdyAlterHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<KkdyAlterBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<KkdyAlterLegs>();
		}

		private void ApplyArmorAttackSpeed() {
			if (KkdyAlterHelmetActive)
				Player.GetAttackSpeed(DamageClass.Ranged) *= 1.15f;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (KkdyAlterSetActive && item.DamageType.CountsAsClass(DamageClass.Ranged) && Main.rand.NextFloat() < 0.2f)
				damage *= 1.5f;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryStunOnRangedCrit(item.DamageType, target, hit, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryStunOnRangedCrit(proj.DamageType, target, hit, damageDone);
		}

		private void TryStunOnRangedCrit(DamageClass damageType, NPC target, NPC.HitInfo hit, int damageDone) {
			if (!KkdyAlterSetActive || !hit.Crit || damageDone <= 0 || !damageType.CountsAsClass(DamageClass.Ranged))
				return;

			OperatorStunNPC.TryApply(target, 12);
		}

		public override void PostUpdateMiscEffects() {
			ApplyArmorAttackSpeed();
		}
	}
}
