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

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			TryRangedProc(item.DamageType, target, ref modifiers);
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			TryRangedProc(proj.DamageType, target, ref modifiers);
		}

		private void TryRangedProc(DamageClass damageType, NPC target, ref NPC.HitModifiers modifiers) {
			if (!KkdyAlterSetActive || !damageType.CountsAsClass(DamageClass.Ranged)
				|| Main.rand.NextFloat() >= 0.2f)
				return;

			// 每次命中只判定一次，同一次触发同时获得增伤和眩晕，与普通暴击独立。
			modifiers.SourceDamage *= 1.5f;
			OperatorStunNPC.TryApply(target, 12);
		}

		public override void PostUpdateMiscEffects() {
			ApplyArmorAttackSpeed();
		}
	}
}
