using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Plume
{
	internal class PlumeSetPlayer : ArknightsArmorPlayer
	{
		public bool PlumeHelmetActive;
		public bool PlumeSetActive;

		public override void ResetEffects() {
			PlumeHelmetActive = false;
			PlumeSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 PlumeHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			PlumeHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<PlumeHead>();
			PlumeSetActive = PlumeHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<PlumeBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<PlumeLegs>();
		}

		private void ApplyArmorDamageStats() {
			if (PlumeHelmetActive)
				Player.GetDamage(DamageClass.Melee) *= 1.03f;
		}

		private void ApplyArmorAttackSpeed() {
			if (PlumeSetActive)
				Player.GetAttackSpeed(DamageClass.Melee) += 0.1f;
		}

		public override void PostUpdateRunSpeeds() {
			if (!PlumeSetActive)
				return;

			// 在实际移动前调整，按描述增加10%加速度与速度上限，避免moveSpeed重复放大。
			Player.runAcceleration *= 1.1f;
			Player.maxRunSpeed *= 1.1f;
			Player.accRunSpeed *= 1.1f;
		}

		public override void PostUpdateMiscEffects() {
			ApplyArmorDamageStats();
			ApplyArmorAttackSpeed();
		}
	}
}
