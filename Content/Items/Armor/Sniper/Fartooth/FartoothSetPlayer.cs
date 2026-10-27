using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fartooth
{
	internal class FartoothSetPlayer : ArknightsArmorPlayer
	{
		public bool FartoothHelmetActive;
		public bool FartoothSetActive;

		private const int NoDamageTicksRequired = 10 * 60;
		private int noDamageTimer;

		public override void ResetEffects() {
			FartoothHelmetActive = false;
			FartoothSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 FartoothHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			FartoothHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<FartoothHead>();
			FartoothSetActive = FartoothHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<FartoothBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<FartoothLegs>();

			if (!FartoothSetActive || Player.dead) {
				noDamageTimer = 0;
				return;
			}

			if (noDamageTimer < NoDamageTicksRequired)
				noDamageTimer++;

			// 两项独立加算：无伤 +20%，首领在场 +30%，同时满足时合计 +50%。
			if (noDamageTimer >= NoDamageTicksRequired)
				Player.GetDamage(DamageClass.Ranged) += 0.2f;
			if (OperatorSetBossHelper.AnyBossActive())
				Player.GetDamage(DamageClass.Ranged) += 0.3f;
		}

		private bool SkillActive => Player.GetModPlayer<WeaponPlayer>().SkillActive;

		public override void PostUpdate() {
			if (FartoothHelmetActive && SkillActive)
				Player.aggro -= 750;
		}

		// 实际受伤才重置计时；闪避的攻击不会打断无伤奖励。
		public override void OnHurt(Player.HurtInfo info) => noDamageTimer = 0;

		public override void UpdateDead() => noDamageTimer = 0;
	}
}
